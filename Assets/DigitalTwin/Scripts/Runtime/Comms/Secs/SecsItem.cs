using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalTwin.Comms.Secs
{
    /// <summary>SECS-II 데이터 아이템 포맷 코드(6-bit). 값 = 포맷코드(octal 표기의 십진).</summary>
    public enum SecsFormat
    {
        List = 0,
        Binary = 0x08, Boolean = 0x09, ASCII = 0x10,
        I8 = 0x18, I1 = 0x19, I2 = 0x1A, I4 = 0x1C,
        F8 = 0x20, F4 = 0x24,
        U8 = 0x28, U1 = 0x29, U2 = 0x2A, U4 = 0x2C
    }

    /// <summary>
    /// SECS-II 아이템 트리. 리스트(L)와 스칼라/배열 값을 표현하고 바이트로 인코딩/디코딩한다.
    /// 포맷 바이트 = (포맷코드 &lt;&lt; 2) | 길이바이트수(1~3), 이어서 길이(빅엔디안), 데이터. (교육용 서브셋)
    /// </summary>
    public class SecsItem
    {
        public SecsFormat Format;
        public List<SecsItem> Items;   // List 용
        public byte[] Raw;             // 스칼라/배열 데이터(빅엔디안)
        public int Count;              // List=자식수, 그 외=값(바이트) 길이

        // ── 생성 헬퍼 ──
        public static SecsItem L(params SecsItem[] children)
            => new SecsItem { Format = SecsFormat.List, Items = new List<SecsItem>(children ?? new SecsItem[0]), Count = children != null ? children.Length : 0 };

        public static SecsItem A(string s)
        {
            var b = Encoding.ASCII.GetBytes(s ?? "");
            return new SecsItem { Format = SecsFormat.ASCII, Raw = b, Count = b.Length };
        }

        public static SecsItem Bool(bool v)
            => new SecsItem { Format = SecsFormat.Boolean, Raw = new byte[] { (byte)(v ? 1 : 0) }, Count = 1 };

        public static SecsItem B(params byte[] v)
            => new SecsItem { Format = SecsFormat.Binary, Raw = v ?? new byte[0], Count = v != null ? v.Length : 0 };

        public static SecsItem U4(params uint[] v)
        {
            var b = new byte[v.Length * 4];
            for (int i = 0; i < v.Length; i++)
            { b[i*4]=(byte)(v[i]>>24); b[i*4+1]=(byte)(v[i]>>16); b[i*4+2]=(byte)(v[i]>>8); b[i*4+3]=(byte)v[i]; }
            return new SecsItem { Format = SecsFormat.U4, Raw = b, Count = v.Length };
        }

        public static SecsItem U2(params ushort[] v)
        {
            var b = new byte[v.Length * 2];
            for (int i = 0; i < v.Length; i++) { b[i*2]=(byte)(v[i]>>8); b[i*2+1]=(byte)v[i]; }
            return new SecsItem { Format = SecsFormat.U2, Raw = b, Count = v.Length };
        }

        public static SecsItem F4(params float[] v)
        {
            var b = new byte[v.Length * 4];
            for (int i = 0; i < v.Length; i++)
            {
                var fb = BitConverter.GetBytes(v[i]);
                if (BitConverter.IsLittleEndian) Array.Reverse(fb);
                Array.Copy(fb, 0, b, i*4, 4);
            }
            return new SecsItem { Format = SecsFormat.F4, Raw = b, Count = v.Length };
        }

        // ── 접근 헬퍼 ──
        public string AsAscii() => Raw != null ? Encoding.ASCII.GetString(Raw) : "";

        public uint AsU4()
        {
            if (Raw == null) return 0;
            uint v = 0; foreach (var b in Raw) v = (v << 8) | b; return v;
        }

        public float AsF4()
        {
            if (Raw == null || Raw.Length < 4) return 0f;
            var fb = new byte[4]; Array.Copy(Raw, 0, fb, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(fb);
            return BitConverter.ToSingle(fb, 0);
        }

        // ── 인코딩 ──
        public void Encode(List<byte> outp)
        {
            int len = Format == SecsFormat.List ? (Items != null ? Items.Count : 0) : (Raw != null ? Raw.Length : 0);
            int nlen = len > 0xFFFF ? 3 : (len > 0xFF ? 2 : 1);
            outp.Add((byte)(((int)Format << 2) | nlen));
            if (nlen == 3) outp.Add((byte)(len >> 16));
            if (nlen >= 2) outp.Add((byte)(len >> 8));
            outp.Add((byte)len);
            if (Format == SecsFormat.List)
            {
                if (Items != null) foreach (var c in Items) c.Encode(outp);
            }
            else if (Raw != null) outp.AddRange(Raw);
        }

        public byte[] Encode()
        {
            var l = new List<byte>();
            Encode(l);
            return l.ToArray();
        }

        // ── 디코딩 ──
        public static SecsItem Decode(byte[] data, ref int pos)
        {
            byte fmtByte = data[pos++];
            int nlen = fmtByte & 0x03;
            int fmt  = fmtByte >> 2;
            int len  = 0;
            for (int i = 0; i < nlen; i++) len = (len << 8) | data[pos++];

            var it = new SecsItem { Format = (SecsFormat)fmt, Count = len };
            if ((SecsFormat)fmt == SecsFormat.List)
            {
                it.Items = new List<SecsItem>(len);
                for (int i = 0; i < len; i++) it.Items.Add(Decode(data, ref pos));
            }
            else
            {
                it.Raw = new byte[len];
                Array.Copy(data, pos, it.Raw, 0, len);
                pos += len;
            }
            return it;
        }
    }
}
