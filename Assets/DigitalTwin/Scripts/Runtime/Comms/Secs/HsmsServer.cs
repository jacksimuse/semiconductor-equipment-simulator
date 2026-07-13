using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DigitalTwin.Comms.Secs
{
    /// <summary>HSMS 메시지: 10바이트 헤더 + SECS-II 바디. (앞의 4바이트 길이 접두는 프레이밍에서 처리)</summary>
    public class HsmsMessage
    {
        public ushort SessionId;
        public byte   Stream;      // 데이터 메시지 스트림
        public byte   Function;    // 데이터 메시지 펑션
        public bool   WBit;        // 응답 요구
        public byte   SType;       // 0=data,1=Select.req,2=Select.rsp,5=Linktest.req,6=Linktest.rsp,9=Separate
        public uint   SystemBytes; // 트랜잭션 ID
        public byte[] Body;        // SECS-II 인코딩(데이터 메시지)

        public bool IsData => SType == 0;

        /// <summary>4바이트 길이 접두 포함 전체 프레임 바이트.</summary>
        public byte[] ToBytes()
        {
            int bodyLen = Body != null ? Body.Length : 0;
            int msgLen  = 10 + bodyLen;
            var buf = new byte[4 + msgLen];
            buf[0]=(byte)(msgLen>>24); buf[1]=(byte)(msgLen>>16); buf[2]=(byte)(msgLen>>8); buf[3]=(byte)msgLen;
            buf[4]=(byte)(SessionId>>8); buf[5]=(byte)SessionId;
            if (IsData) { buf[6]=(byte)((WBit?0x80:0) | (Stream & 0x7F)); buf[7]=Function; }
            else        { buf[6]=0; buf[7]=0; }   // 제어 메시지(우리 응답은 status 0)
            buf[8]=0;      // PType (SECS-II)
            buf[9]=SType;
            buf[10]=(byte)(SystemBytes>>24); buf[11]=(byte)(SystemBytes>>16); buf[12]=(byte)(SystemBytes>>8); buf[13]=(byte)SystemBytes;
            if (bodyLen>0) Array.Copy(Body, 0, buf, 14, bodyLen);
            return buf;
        }

        /// <summary>길이 접두를 제외한 프레임(헤더+바디)에서 메시지를 복원.</summary>
        public static HsmsMessage FromFrame(byte[] h)
        {
            var m = new HsmsMessage();
            m.SessionId   = (ushort)((h[0]<<8) | h[1]);
            m.SType       = h[5];
            m.SystemBytes = (uint)((h[6]<<24) | (h[7]<<16) | (h[8]<<8) | h[9]);
            if (m.SType == 0)
            {
                m.WBit     = (h[2] & 0x80) != 0;
                m.Stream   = (byte)(h[2] & 0x7F);
                m.Function = h[3];
                if (h.Length > 10) { m.Body = new byte[h.Length-10]; Array.Copy(h, 10, m.Body, 0, h.Length-10); }
            }
            return m;
        }
    }

    /// <summary>
    /// 최소 HSMS-SS 서버(장비 측, passive). 단일 세션.
    ///   - Select.req/Linktest.req 는 내부에서 자동 응답.
    ///   - 데이터 메시지(SxFy)는 OnData 델리게이트로 넘겨 응답을 받는다(백그라운드 스레드).
    ///   - SendRaw 로 비요청 메시지(이벤트/알람)를 밀어넣을 수 있다.
    /// Unity API 는 절대 이 스레드에서 만지지 말 것 — 스냅샷/큐로 교신.
    /// </summary>
    public class HsmsServer
    {
        readonly int port;
        TcpListener listener;
        Thread acceptThread;
        volatile bool running;
        NetworkStream clientStream;
        readonly object sendLock = new object();

        public Func<HsmsMessage, HsmsMessage> OnData; // 데이터 메시지 처리(응답 반환/ null)
        public Action<string> Log;
        public bool ClientConnected { get; private set; }

        public HsmsServer(int port) { this.port = port; }

        public void Start()
        {
            running = true;
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "HSMS-accept" };
            acceptThread.Start();
            if (Log != null) Log("listening on " + port);
        }

        public void Stop()
        {
            running = false;
            try { if (clientStream != null) clientStream.Close(); } catch {}
            try { if (listener != null) listener.Stop(); } catch {}
        }

        void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    clientStream = client.GetStream();
                    ClientConnected = true;
                    if (Log != null) Log("client connected");
                    RecvLoop(client);
                }
                catch (Exception e) { if (running && Log != null) Log("accept err: " + e.Message); }
                clientStream = null;
                ClientConnected = false;
            }
        }

        void RecvLoop(TcpClient client)
        {
            var stream = client.GetStream();
            try
            {
                while (running)
                {
                    var lenBuf = ReadExact(stream, 4);
                    if (lenBuf == null) break;
                    int msgLen = (lenBuf[0]<<24)|(lenBuf[1]<<16)|(lenBuf[2]<<8)|lenBuf[3];
                    var frame = ReadExact(stream, msgLen);
                    if (frame == null) break;
                    HandleIncoming(HsmsMessage.FromFrame(frame));
                }
            }
            catch (Exception e) { if (running && Log != null) Log("recv err: " + e.Message); }
            try { client.Close(); } catch {}
        }

        void HandleIncoming(HsmsMessage m)
        {
            switch (m.SType)
            {
                case 1: // Select.req → Select.rsp
                    SendRaw(new HsmsMessage { SessionId = m.SessionId, SType = 2, SystemBytes = m.SystemBytes });
                    return;
                case 5: // Linktest.req → Linktest.rsp
                    SendRaw(new HsmsMessage { SessionId = 0xFFFF, SType = 6, SystemBytes = m.SystemBytes });
                    return;
                case 9: // Separate
                    return;
                case 0: // data
                    var reply = OnData != null ? OnData(m) : null;
                    if (reply != null) SendRaw(reply);
                    return;
            }
        }

        public void SendRaw(HsmsMessage m)
        {
            lock (sendLock)
            {
                var s = clientStream;
                if (s == null) return;   // 호스트 미연결 → 조용히 무시
                try { var b = m.ToBytes(); s.Write(b, 0, b.Length); }
                catch (Exception e) { clientStream = null; ClientConnected = false; if (Log != null) Log("send err: " + e.Message); }
            }
        }

        static byte[] ReadExact(NetworkStream s, int n)
        {
            var buf = new byte[n]; int off = 0;
            while (off < n) { int r = s.Read(buf, off, n - off); if (r <= 0) return null; off += r; }
            return buf;
        }
    }
}
