import socket
import struct

# Message types
MT_INIT     = 0
MT_EXIT     = 1
MT_GETDATA  = 2
MT_DATA     = 3
MT_NODATA   = 4
MT_CONFIRM  = 5

# Message recipients
MR_BROKER   = 10
MR_ALL      = 50
MR_USER     = 100

# Header: 4 x int32 little-endian = 16 bytes
HEADER_FMT  = '<iiii'
HEADER_SIZE = struct.calcsize(HEADER_FMT)  # 16


def _recv_exact(s, n):
    """Receive exactly n bytes from socket. Returns None on disconnect."""
    buf = b''
    while len(buf) < n:
        try:
            chunk = s.recv(n - len(buf))
        except (socket.timeout, OSError):
            return None
        if not chunk:
            return None
        buf += chunk
    return buf


class MsgHeader:
    __slots__ = ('type', 'size', 'to', 'from_id')

    def __init__(self, msg_type=0, size=0, to=0, from_id=0):
        self.type = msg_type
        self.size = size
        self.to = to
        self.from_id = from_id

    def send(self, s):
        packet = struct.pack(HEADER_FMT, self.type, self.size, self.to, self.from_id)
        s.sendall(packet)

    def receive(self, s):
        data = _recv_exact(s, HEADER_SIZE)
        if data is None:
            self.type = MT_NODATA
            self.size = 0
            return
        self.type, self.size, self.to, self.from_id = struct.unpack(HEADER_FMT, data)


class Message:
    """Protocol message with header + UTF-16LE encoded data payload."""

    # Shared client ID (legacy convenience; modern code reads it from header.to)
    ClientID = 0

    def __init__(self, to=0, from_id=0, msg_type=MT_DATA, data=""):
        encoded = data.encode('utf-16-le') if data else b''
        self.header = MsgHeader(msg_type, len(encoded), to, from_id)
        self.data = data

    def send(self, s):
        self.header.send(s)
        if self.header.size > 0:
            s.sendall(self.data.encode('utf-16-le'))

    def receive(self, s):
        self.header.receive(s)
        if self.header.size > 0:
            raw = _recv_exact(s, self.header.size)
            if raw is not None:
                self.data = raw.decode('utf-16-le')
            else:
                self.data = ""
        else:
            self.data = ""
