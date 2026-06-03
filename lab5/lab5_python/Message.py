import socket
import struct
import sys

# Message types
MT_INIT     = 0
MT_EXIT     = 1
MT_GETDATA  = 2
MT_DATA     = 3
MT_NODATA   = 4
MT_CONFIRM  = 5

# Message recipients
MR_BROKER = 10
MR_ALL    = 50
MR_USER   = 100

# Header: type(4) + size(4) + to(4) + from(4) = 16 bytes
HEADER_FORMAT = 'iiii'
HEADER_SIZE = struct.calcsize(HEADER_FORMAT)  # 16


class MsgHeader:
    def __init__(self, msg_type=0, size=0, to=0, from_id=0):
        self.type = msg_type
        self.size = size
        self.to = to
        self.from_id = from_id

    def send(self, s):
        s.sendall(struct.pack(HEADER_FORMAT, self.type, self.size, self.to, self.from_id))

    def receive(self, s):
        data = _recv_exact(s, HEADER_SIZE)
        if data is None:
            self.type = MT_NODATA
            self.size = 0
            return
        self.type, self.size, self.to, self.from_id = struct.unpack(HEADER_FORMAT, data)


class Message:
    ClientID = 0

    def __init__(self, to=0, from_id=0, msg_type=MT_DATA, data=""):
        self.header = MsgHeader(msg_type, len(data) * 2, to, from_id)
        self.data = data

    def send(self, s):
        self.header.send(s)
        if self.header.size > 0:
            s.sendall(struct.pack(f'{self.header.size}s', self.data.encode('utf-16-le')))

    def receive(self, s):
        self.header.receive(s)
        if self.header.size > 0:
            raw = _recv_exact(s, self.header.size)
            if raw is not None:
                self.data = struct.unpack(f'{self.header.size}s', raw)[0].decode('utf-16-le')
            else:
                self.data = ""
        else:
            self.data = ""

    @staticmethod
    def send_message(to, msg_type=MT_DATA, data=""):
        host = sys.argv[1] if len(sys.argv) > 1 else 'localhost'
        port = 12345
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect((host, port))
            m = Message(to, Message.ClientID, msg_type, data)
            m.send(s)
            m.receive(s)
            if m.header.type == MT_INIT:
                Message.ClientID = m.header.to
            return m


def _recv_exact(s, n):
    """Receive exactly n bytes from socket."""
    buf = b''
    while len(buf) < n:
        chunk = s.recv(n - len(buf))
        if not chunk:
            return None
        buf += chunk
    return buf