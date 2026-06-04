# Salakhova Lab 5 — SalakhovaClient class with persistent socket, background reader, interactive loop
import argparse
import socket
import struct
import threading
import time
from queue import Queue, Empty
from Salakhova_Message import (
    MT_INIT, MT_EXIT, MT_GETDATA, MT_DATA, MT_NODATA,
    MR_BROKER, MR_ALL,
    MsgHeader, Message,
    HEADER_FMT, HEADER_SIZE,
    _recv_exact,
)


class SalakhovaClient:
    """Persistent-connection message broker client.

    Connects to the broker once, keeps the socket open, and runs a
    background reader thread that polls for incoming messages via
    MT_GETDATA / MT_NODATA.
    """

    def __init__(self, host='localhost', port=12345, name='client'):
        self.host = host
        self.port = port
        self.name = name
        self._sock = None
        self._lock = threading.Lock()
        self._inbox = Queue()
        self._reader_thread = None
        self._stop_event = threading.Event()
        self._client_id = 0
        self._connected = False

    # ------------------------------------------------------------------
    # Properties
    # ------------------------------------------------------------------

    @property
    def client_id(self):
        return self._client_id

    @property
    def is_connected(self):
        return self._connected

    # ------------------------------------------------------------------
    # Connection lifecycle
    # ------------------------------------------------------------------

    def connect(self):
        """Open socket, register with broker, start background reader."""
        if self._connected:
            return

        self._sock = socket.create_connection((self.host, self.port))
        self._sock.settimeout(None)  # blocking mode

        # Send MT_INIT with user name
        msg = Message(MR_BROKER, 0, MT_INIT, self.name)
        with self._lock:
            msg.send(self._sock)
            reply = Message()
            reply.receive(self._sock)

        if reply.header.type == MT_INIT:
            self._client_id = reply.header.to
            self._connected = True
        else:
            self._sock.close()
            self._sock = None
            raise ConnectionError(
                f"Unexpected response type {reply.header.type} from broker"
            )

        # Start background reader
        self._stop_event.clear()
        self._reader_thread = threading.Thread(
            target=self._reader_loop, daemon=True
        )
        self._reader_thread.start()

    def disconnect(self):
        """Send MT_EXIT and close the socket."""
        if not self._connected:
            return

        self._stop_event.set()
        try:
            with self._lock:
                msg = Message(MR_BROKER, self._client_id, MT_EXIT)
                msg.send(self._sock)
        except OSError:
            pass

        try:
            self._sock.close()
        except OSError:
            pass

        self._sock = None
        self._connected = False

        if self._reader_thread and self._reader_thread.is_alive():
            self._reader_thread.join(timeout=1.0)

    # ------------------------------------------------------------------
    # Sending (thread-safe)
    # ------------------------------------------------------------------

    def send_message(self, to, msg_type=MT_DATA, data=""):
        """Send a message on the persistent connection.

        Thread-safe via internal lock.
        """
        if not self._connected:
            raise ConnectionError("Not connected to broker")

        msg = Message(to, self._client_id, msg_type, data)
        with self._lock:
            msg.send(self._sock)

    # ------------------------------------------------------------------
    # Receiving
    # ------------------------------------------------------------------

    def try_receive(self):
        """Non-blocking pop from the inbox queue.

        Returns a Message or None if the inbox is empty.
        """
        try:
            return self._inbox.get_nowait()
        except Empty:
            return None

    # ------------------------------------------------------------------
    # Background reader
    # ------------------------------------------------------------------

    def _reader_loop(self):
        """Background thread: poll for incoming messages until told to stop.

        Sends an MT_GETDATA request, reads the reply, and enqueues
        MT_DATA messages into the inbox. Sleeps briefly on MT_NODATA.
        """
        while not self._stop_event.is_set():
            try:
                # Poll for messages
                with self._lock:
                    get_msg = Message(MR_BROKER, self._client_id, MT_GETDATA)
                    get_msg.send(self._sock)
                    reply = Message()
                    reply.receive(self._sock)

                if reply.header.type == MT_DATA:
                    self._inbox.put(reply)
                elif reply.header.type == MT_NODATA:
                    time.sleep(0.1)
                elif reply.header.type == MT_CONFIRM:
                    # Acknowledge — nothing to do
                    pass
                else:
                    # Unexpected — slow down to avoid busy-loop
                    time.sleep(0.1)

            except (ConnectionError, OSError, struct.error, EOFError):
                self._connected = False
                break


# ======================================================================
# Console UI
# ======================================================================

def _inbox_printer(client):
    """Background thread: print incoming messages as they arrive."""
    while client.is_connected:
        msg = client.try_receive()
        if msg is not None:
            sender = msg.header.from_id
            text = msg.data
            print(f"\n[From {sender}]: {text}\n> ", end="", flush=True)
        else:
            # No message — brief sleep to avoid busy-wait
            time.sleep(0.05)


def main():
    parser = argparse.ArgumentParser(
        description="Salakhova Lab 5 — Persistent-connection message broker client"
    )
    parser.add_argument('host', nargs='?', default='localhost',
                        help='Broker hostname (default: localhost)')
    parser.add_argument('port', nargs='?', type=int, default=12345,
                        help='Broker port (default: 12345)')
    parser.add_argument('-n', '--name', default='Salakhova',
                        help='Display name for this client (default: Salakhova)')
    args = parser.parse_args()

    client = SalakhovaClient(args.host, args.port, args.name)

    print("Connecting to broker...")
    try:
        client.connect()
    except (ConnectionError, OSError) as e:
        print(f"Cannot connect to {args.host}:{args.port} — {e}")
        return

    print(f"Connected! Your client ID: {client.client_id}")
    print("Commands:")
    print("  exit          — disconnect and quit")
    print("  all <text>    — broadcast message to everyone")
    print("  <id> <text>   — send private message to client <id>")
    print("=" * 50)

    # Start inbox printer in background
    printer = threading.Thread(
        target=_inbox_printer, args=(client,), daemon=True
    )
    printer.start()

    try:
        while client.is_connected:
            try:
                line = input("> ").strip()
            except (EOFError, KeyboardInterrupt):
                print()
                break

            if not line:
                continue

            if line.lower() == 'exit':
                break

            # Parse "all <text>" or "<numeric_id> <text>"
            first_space = line.find(' ')
            if first_space == -1:
                print("Usage: all <text>  or  <id> <text>")
                continue

            target = line[:first_space]
            text = line[first_space + 1:]

            if not text:
                continue

            if target.lower() == 'all':
                recipient = MR_ALL
            else:
                try:
                    recipient = int(target)
                except ValueError:
                    print(f"Invalid recipient '{target}'. Use 'all' or a numeric ID.")
                    continue

            try:
                client.send_message(recipient, MT_DATA, text)
            except ConnectionError as e:
                print(f"Send failed: {e}")
                break

    finally:
        client.disconnect()
        print("Disconnected.")


if __name__ == '__main__':
    main()