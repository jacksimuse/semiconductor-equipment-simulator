using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalTwin.Comms.Secs
{
    /// <summary>
    /// Unity 를 가상 GEM 장비로 만드는 컴포넌트. HSMS(:port) 서버를 열고
    /// 호스트의 SECS/GEM 메시지를 백엔드 호출로 변환한다.
    ///   S1F1/F2(Are You There) · S1F13/F14(통신확립) · S1F3/F4(SVID 조회) ·
    ///   S2F41/F42(원격명령 START/STOP/HOME) · S6F11(이벤트) · S5F1(알람=충돌 E-stop)
    /// Unity API 접근은 메인 스레드(Update)에서만. HSMS 스레드는 스냅샷/큐로만 교신.
    /// </summary>
    [RequireComponent(typeof(EquipmentLink))]
    public class GemEquipment : MonoBehaviour
    {
        [Header("HSMS")]
        public int    port     = 5000;
        public ushort deviceId = 1;
        public bool   autoStart = true;

        [Header("장비 식별")]
        public string model   = "DT-ROBOT";
        public string softRev = "1.0";

        EquipmentLink link;
        HsmsServer    server;

        // ── 스냅샷 (메인 → HSMS 스레드 읽기) ──
        readonly object snapLock = new object();
        readonly float[] snapJoints = new float[6];
        Vector3 snapTcp;
        bool snapRunning, snapEstop;
        int  snapWafers;

        // ── 원격명령 큐 (HSMS → 메인) ──
        readonly ConcurrentQueue<string> rcmdQueue = new ConcurrentQueue<string>();

        bool prevRunning, prevEstop;

        void Awake() => link = GetComponent<EquipmentLink>();

        void OnEnable()  { if (autoStart) StartServer(); }
        void OnDisable() { if (server != null) { server.Stop(); server = null; } }

        public void StartServer()
        {
            if (server != null) return;
            server = new HsmsServer(port);
            server.OnData = HandleData;   // 백그라운드 스레드
            server.Log = s => MainThreadDispatcher.Enqueue(() => Debug.Log("[GEM] " + s));
            server.Start();
        }

        void Update()
        {
            var be = link != null ? link.Backend : null;
            if (be == null) return;

            // 스냅샷 갱신
            var js = be.ReadJoints();
            var st = be.ReadStatus();
            lock (snapLock)
            {
                if (js.joints != null)
                    for (int i = 0; i < 6 && i < js.joints.Length; i++) snapJoints[i] = js.joints[i];
                snapTcp = js.tcpPos; snapRunning = st.running; snapEstop = st.eStop; snapWafers = st.waferCount;
            }

            // 원격명령 적용 (메인 스레드)
            string rcmd;
            while (rcmdQueue.TryDequeue(out rcmd))
                Debug.Log($"[GEM] RCMD {rcmd} → {be.CommandRemote(rcmd)}");

            // 이벤트/알람 엣지 감지 → 비요청 리포트
            if (st.eStop && !prevEstop) SendAlarm(1, "COLLISION_ESTOP");
            if (!st.running && prevRunning) SendEvent(20, "CYCLE_COMPLETE");
            prevEstop = st.eStop; prevRunning = st.running;
        }

        // ── HSMS 데이터 처리 (백그라운드 스레드) ──
        HsmsMessage HandleData(HsmsMessage m)
        {
            try
            {
                if (m.Stream == 1 && m.Function == 1)  return Reply(m, 1, 2,  S1F2());
                if (m.Stream == 1 && m.Function == 13) return Reply(m, 1, 14, S1F14());
                if (m.Stream == 1 && m.Function == 3)  return Reply(m, 1, 4,  S1F4(m.Body));
                if (m.Stream == 2 && m.Function == 41) return Reply(m, 2, 42, S2F42(m.Body));
            }
            catch (Exception e) { if (server != null && server.Log != null) server.Log("handle err: " + e.Message); }
            return null;
        }

        SecsItem S1F2()  => SecsItem.L(SecsItem.A(model), SecsItem.A(softRev));
        SecsItem S1F14() => SecsItem.L(SecsItem.B(0), SecsItem.L(SecsItem.A(model), SecsItem.A(softRev)));

        SecsItem S1F4(byte[] body)
        {
            float[] j; Vector3 tcp; bool run, est; int waf;
            lock (snapLock) { j = (float[])snapJoints.Clone(); tcp = snapTcp; run = snapRunning; est = snapEstop; waf = snapWafers; }

            var vals = new List<SecsItem>();
            if (body != null && body.Length > 0)
            {
                int pos = 0; var req = SecsItem.Decode(body, ref pos);
                if (req.Items != null)
                    foreach (var s in req.Items) vals.Add(SvidValue(s.AsU4(), j, tcp, run, est, waf));
            }
            return new SecsItem { Format = SecsFormat.List, Items = vals, Count = vals.Count };
        }

        static SecsItem SvidValue(uint svid, float[] j, Vector3 tcp, bool run, bool est, int waf)
        {
            if (svid >= 1 && svid <= 6) return SecsItem.F4(j[svid - 1]);
            switch (svid)
            {
                case 10: return SecsItem.Bool(run);
                case 11: return SecsItem.Bool(est);
                case 12: return SecsItem.U4((uint)waf);
                case 20: return SecsItem.F4(tcp.x);
                case 21: return SecsItem.F4(tcp.y);
                case 22: return SecsItem.F4(tcp.z);
                default: return SecsItem.A("");
            }
        }

        SecsItem S2F42(byte[] body)
        {
            string rcmd = "";
            if (body != null && body.Length > 0)
            {
                int pos = 0; var root = SecsItem.Decode(body, ref pos);
                if (root.Items != null && root.Items.Count >= 1) rcmd = root.Items[0].AsAscii();
            }
            rcmd = (rcmd ?? "").Trim().ToUpperInvariant();
            byte hcack = 0;
            if (rcmd == "START" || rcmd == "STOP" || rcmd == "HOME") rcmdQueue.Enqueue(rcmd);
            else hcack = 2; // 2 = command does not exist (GEM HCACK)
            return SecsItem.L(SecsItem.B(hcack), SecsItem.L());
        }

        // ── 비요청 리포트 (메인 스레드에서 호출) ──
        void SendEvent(uint ceid, string name)
        {
            if (server == null) return;
            var rpt = SecsItem.L(SecsItem.U4(0), SecsItem.U4(ceid), SecsItem.L());
            server.SendRaw(Build(6, 11, true, rpt));
            Debug.Log($"[GEM] → S6F11 이벤트 {name} (CEID {ceid})");
        }

        void SendAlarm(uint alid, string text)
        {
            if (server == null) return;
            var al = SecsItem.L(SecsItem.B(0x80), SecsItem.U4(alid), SecsItem.A(text));
            server.SendRaw(Build(5, 1, true, al));
            Debug.Log($"[GEM] → S5F1 알람 {text} (ALID {alid})");
        }

        HsmsMessage Reply(HsmsMessage req, byte s, byte f, SecsItem body)
            => new HsmsMessage { SessionId = deviceId, SType = 0, Stream = s, Function = f, WBit = false, SystemBytes = req.SystemBytes, Body = body != null ? body.Encode() : null };

        uint sysCounter = 1000;
        HsmsMessage Build(byte s, byte f, bool w, SecsItem body)
            => new HsmsMessage { SessionId = deviceId, SType = 0, Stream = s, Function = f, WBit = w, SystemBytes = ++sysCounter, Body = body != null ? body.Encode() : null };
    }
}
