#!/usr/bin/env python3
"""
secs_host.py — Unity 가상 GEM 장비(GemEquipment, HSMS :5000)용 최소 호스트 테스터.

순수 파이썬(외부 의존성 없음). Unity 에서 DigitalTwin 씬을 Play 한 뒤 실행:
    python tools/secs_host.py

흐름: HSMS Select → S1F1(장비 확인) → S1F3(SVID 상태 조회)
      → S2F41 START(원격명령) → 이후 수신되는 S6F11 이벤트 / S5F1 알람을 몇 초간 출력.

참고: 이 장비는 GEM '교육용 서브셋'을 구현합니다. 표준 완결성이 더 필요하면
      파이썬 `secsgem` 라이브러리 호스트로 대체할 수 있으나, 그 경우 장비 측에서
      지원 메시지(S1F13 협상, S9Fx 오류, 링크테스트 주기 등)를 보강해야 합니다.
"""
import socket
import struct

HOST, PORT = "127.0.0.1", 5000

# ── SECS-II 포맷 코드 (교육용 서브셋) ──
FMT_LIST = 0x00
FMT_BIN = 0x08
FMT_BOOL = 0x09
FMT_ASCII = 0x10
FMT_F4 = 0x24
FMT_U4 = 0x2C


def _hdr(fmt, n):
    if n > 0xFFFF:
        nlen, lb = 3, bytes([(n >> 16) & 0xFF, (n >> 8) & 0xFF, n & 0xFF])
    elif n > 0xFF:
        nlen, lb = 2, bytes([(n >> 8) & 0xFF, n & 0xFF])
    else:
        nlen, lb = 1, bytes([n & 0xFF])
    return bytes([(fmt << 2) | nlen]) + lb


def L(*items):
    return _hdr(FMT_LIST, len(items)) + b"".join(items)


def A(s):
    b = s.encode("ascii")
    return _hdr(FMT_ASCII, len(b)) + b


def U4(*vals):
    b = b"".join(struct.pack(">I", v) for v in vals)
    return _hdr(FMT_U4, len(vals)) + b


def decode(data, pos=0):
    fmtb = data[pos]; pos += 1
    nlen = fmtb & 0x03
    fmt = fmtb >> 2
    n = 0
    for _ in range(nlen):
        n = (n << 8) | data[pos]; pos += 1
    if fmt == FMT_LIST:
        items = []
        for _ in range(n):
            it, pos = decode(data, pos)
            items.append(it)
        return (("L", items), pos)
    raw = data[pos:pos + n]; pos += n
    if fmt == FMT_ASCII:
        return (("A", raw.decode("ascii", "replace")), pos)
    if fmt == FMT_U4:
        return (("U4", [struct.unpack(">I", raw[i:i + 4])[0] for i in range(0, n, 4)]), pos)
    if fmt == FMT_F4:
        return (("F4", [round(struct.unpack(">f", raw[i:i + 4])[0], 3) for i in range(0, n, 4)]), pos)
    if fmt == FMT_BOOL:
        return (("BOOL", list(raw)), pos)
    if fmt == FMT_BIN:
        return (("B", list(raw)), pos)
    return (("?", list(raw)), pos)


# ── HSMS 프레이밍 ──
def hsms(session, b2, b3, stype, sysbytes, body=b""):
    header = struct.pack(">HBBBBI", session, b2, b3, 0, stype, sysbytes)
    return struct.pack(">I", len(header) + len(body)) + header + body


def data_msg(stream, func, sysbytes, body=b"", wbit=True):
    b2 = (0x80 if wbit else 0) | (stream & 0x7F)
    return hsms(1, b2, func, 0, sysbytes, body)


def recvn(sock, n):
    buf = b""
    while len(buf) < n:
        chunk = sock.recv(n - len(buf))
        if not chunk:
            return None
        buf += chunk
    return buf


def recv_frame(sock):
    hdr = recvn(sock, 4)
    if not hdr:
        return None
    (ml,) = struct.unpack(">I", hdr)
    msg = recvn(sock, ml)
    if not msg:
        return None
    session, b2, b3, ptype, stype, sysbytes = struct.unpack(">HBBBBI", msg[:10])
    return dict(session=session, stype=stype, stream=b2 & 0x7F, func=b3,
                wbit=bool(b2 & 0x80), sysbytes=sysbytes, body=msg[10:])


def main():
    s = socket.create_connection((HOST, PORT), timeout=3)
    print(f"[host] 연결됨 {HOST}:{PORT}")

    s.sendall(hsms(0xFFFF, 0, 0, 1, 1))                 # Select.req
    r = recv_frame(s); print("[host] Select.rsp stype =", r["stype"], "(기대 2)")

    s.sendall(data_msg(1, 1, 2))                         # S1F1 Are You There
    r = recv_frame(s); print("[host] S1F2 장비ID:", decode(r["body"])[0])

    svids = L(U4(1), U4(2), U4(3), U4(10), U4(12))       # J1,J2,J3,running,wafers
    s.sendall(data_msg(1, 3, 3, svids))                  # S1F3 상태 조회
    r = recv_frame(s); print("[host] S1F4 상태:", decode(r["body"])[0])

    s.sendall(data_msg(2, 41, 4, L(A("START"), L())))    # S2F41 원격명령 START
    r = recv_frame(s); print("[host] S2F42 HCACK:", decode(r["body"])[0], "(0=수락)")

    print("[host] 이벤트/알람 수신 대기(5초)...")
    s.settimeout(5)
    try:
        while True:
            r = recv_frame(s)
            if r is None:
                break
            body = decode(r["body"])[0] if r["body"] else None
            print(f"[host]   ← S{r['stream']}F{r['func']}", body)
    except socket.timeout:
        print("[host] (수신 종료)")
    s.close()


if __name__ == "__main__":
    main()
