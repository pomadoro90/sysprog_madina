using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Salakhova_Sharp
{
    public enum MessageTypes : int
    {
        MT_INIT = 0,
        MT_EXIT = 1,
        MT_GETDATA = 2,
        MT_DATA = 3,
        MT_NODATA = 4,
        MT_CONFIRM = 5
    }

    public enum MessageRecipients : int
    {
        MR_BROKER = 10,
        MR_ALL = 50,
        MR_USER = 100
    }

    public struct MessageHeader
    {
        public int type;
        public int size;
        public int to;
        public int from;

        public MessageHeader(int type, int size, int to, int from)
        {
            this.type = type;
            this.size = size;
            this.to = to;
            this.from = from;
        }
    }

    public class Message
    {
        public MessageHeader header;
        public string data = "";

        public Message() { }

        public Message(int to, int from, MessageTypes type = MessageTypes.MT_DATA, string data = "")
        {
            this.data = data;
            header = new MessageHeader((int)type, data != null ? data.Length * 2 : 0, to, from);
        }
    }

    public class SalakhovaSocketClient
    {
        private Socket socket;
        private Thread readerThread;
        private volatile bool isRunning;
        private readonly ConcurrentQueue<Message> inbox = new ConcurrentQueue<Message>();
        private readonly object sendLock = new object();
        private int clientId;

        public int ClientId => clientId;
        public bool IsConnected { get; private set; }

        public void Connect(string host, int port)
        {
            if (IsConnected) return;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse(host), port));

            if (!socket.Connected)
                throw new Exception("Connection error");

            // Send MT_INIT to get client ID
            SendMessageRaw(MessageRecipients.MR_BROKER, MessageTypes.MT_INIT, "");

            // Receive the INIT response with client ID
            var initMsg = ReceiveMessageRaw();
            if (initMsg.header.type != (int)MessageTypes.MT_INIT)
                throw new Exception("Unexpected response to INIT");

            clientId = initMsg.header.to;
            IsConnected = true;

            // Start background reader thread
            isRunning = true;
            readerThread = new Thread(ReaderLoop);
            readerThread.IsBackground = true;
            readerThread.Start();
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            isRunning = false;
            IsConnected = false;

            // Send MT_EXIT
            try
            {
                lock (sendLock)
                {
                    if (socket != null && socket.Connected)
                    {
                        SendMessageRaw(MessageRecipients.MR_BROKER, MessageTypes.MT_EXIT, "");
                    }
                }
            }
            catch { }

            try
            {
                readerThread?.Join(500);
            }
            catch { }

            try
            {
                if (socket != null && socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch { }

            socket?.Close();
            socket = null;

            // Clear inbox
            while (inbox.TryDequeue(out _)) { }
        }

        public void Send(int to, MessageTypes type, string data)
        {
            if (!IsConnected) throw new Exception("Not connected");
            lock (sendLock)
            {
                SendMessageRaw((MessageRecipients)to, type, data);
            }
        }

        public bool TryReceive(out Message message)
        {
            return inbox.TryDequeue(out message);
        }

        private void ReaderLoop()
        {
            while (isRunning && IsConnected)
            {
                try
                {
                    // Poll: send MT_GETDATA to broker
                    lock (sendLock)
                    {
                        if (!isRunning || socket == null || !socket.Connected) break;
                        SendMessageRaw(MessageRecipients.MR_BROKER, MessageTypes.MT_GETDATA, "");
                    }

                    // Receive response
                    var msg = ReceiveMessageRaw();

                    if (msg.header.type == (int)MessageTypes.MT_DATA ||
                        msg.header.type == (int)MessageTypes.MT_CONFIRM ||
                        msg.header.type == (int)MessageTypes.MT_INIT)
                    {
                        inbox.Enqueue(msg);
                    }
                    // MT_NODATA is normal - nothing to do

                    Thread.Sleep(100);
                }
                catch
                {
                    IsConnected = false;
                    isRunning = false;
                    break;
                }
            }

            IsConnected = false;
        }

        private byte[] HeaderToBytes(MessageHeader header)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(header.type).CopyTo(bytes, 0);
            BitConverter.GetBytes(header.size).CopyTo(bytes, 4);
            BitConverter.GetBytes(header.to).CopyTo(bytes, 8);
            BitConverter.GetBytes(header.from).CopyTo(bytes, 12);
            return bytes;
        }

        private MessageHeader BytesToHeader(byte[] bytes)
        {
            return new MessageHeader
            {
                type = BitConverter.ToInt32(bytes, 0),
                size = BitConverter.ToInt32(bytes, 4),
                to = BitConverter.ToInt32(bytes, 8),
                from = BitConverter.ToInt32(bytes, 12)
            };
        }

        private void SendMessageRaw(MessageRecipients to, MessageTypes type, string data)
        {
            byte[] dataBytes = data != null ? Encoding.Unicode.GetBytes(data) : Array.Empty<byte>();
            int dataSize = dataBytes.Length;

            var header = new MessageHeader((int)type, dataSize, (int)to, clientId);
            byte[] headerBytes = HeaderToBytes(header);

            socket.Send(headerBytes, 16, SocketFlags.None);
            if (dataSize > 0)
            {
                socket.Send(dataBytes, dataSize, SocketFlags.None);
            }
        }

        private Message ReceiveMessageRaw()
        {
            byte[] headerBytes = new byte[16];
            int received = 0;
            while (received < 16)
            {
                int n = socket.Receive(headerBytes, received, 16 - received, SocketFlags.None);
                if (n == 0) throw new Exception("Connection closed");
                received += n;
            }

            var header = BytesToHeader(headerBytes);
            var msg = new Message();
            msg.header = header;

            if (header.size > 0)
            {
                byte[] dataBytes = new byte[header.size];
                received = 0;
                while (received < header.size)
                {
                    int n = socket.Receive(dataBytes, received, header.size - received, SocketFlags.None);
                    if (n == 0) throw new Exception("Connection closed");
                    received += n;
                }
                msg.data = Encoding.Unicode.GetString(dataBytes, 0, header.size);
            }

            return msg;
        }
    }
}
