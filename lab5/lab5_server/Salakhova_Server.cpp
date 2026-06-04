#include "Salakhova_SysProg.h"
#include "Salakhova_Message.h"
#include "Salakhova_Session.h"
#include "Salakhova_Interfaces.h"

class SocketTransport : public Sender, public Receiver
{
    tcp::socket& s;
    mutable std::mutex writeMx_;

public:
    SocketTransport(tcp::socket& s)
        : s(s)
    {
    }

    virtual void send(Message& m) const override
    {
        std::lock_guard<std::mutex> lg(writeMx_);
        sendData(s, &m.header, sizeof(MessageHeader));
        if (m.header.size > 0)
        {
            sendData(s, m.data.data(), m.header.size);
        }
    }

    virtual void receive(Message& m) const override
    {
        receiveData(s, &m.header, sizeof(MessageHeader));
        if (m.header.size > 0)
        {
            m.data.resize(m.header.size / sizeof(wchar_t));
            receiveData(s, &m.data[0], m.header.size);
        }
        else
        {
            m.data.clear();
        }
    }
};

class SRBroker : public Sender, public Receiver
{
public:
    SRBroker() = default;

    static inline int maxID = MR_USER;
    static inline std::map<int, std::shared_ptr<Session>> sessions;
    static inline std::mutex mx;

    virtual void send(Message& m) const override
    {
        std::lock_guard<std::mutex> lg(mx);
        auto iSessionFrom = sessions.find(m.header.from);
        if (iSessionFrom == sessions.end())
            return;

        if (m.header.to == MR_ALL)
        {
            for (auto& [id, session] : sessions)
            {
                if (id != m.header.from)
                    session->addMessage(m);
            }
        }
        else
        {
            auto iSessionTo = sessions.find(m.header.to);
            if (iSessionTo != sessions.end())
            {
                iSessionTo->second->addMessage(m);
            }
        }
    }

    virtual void receive(Message& m) const override
    {
        std::lock_guard<std::mutex> lg(mx);
        auto iSession = sessions.find(m.header.from);
        if (iSession == sessions.end() || !iSession->second->tryGetMessage(m))
        {
            m = Message(m.header.from, MR_BROKER, MT_NODATA);
        }
    }

    static void worker(tcp::socket s)
    {
        SocketTransport transport(s);

        while (true)
        {
            try
            {
                Message m = Message::receiveMessage(transport);
                SafeWrite(L"msg: to=", m.header.to, L"from=", m.header.from, L"type=", m.header.messageType);

                switch (m.header.messageType)
                {
                    case MT_INIT:
                    {
                        std::lock_guard<std::mutex> lg(mx);
                        int newID = ++maxID;
                        auto session = std::make_shared<Session>(newID, m.data);
                        sessions[newID] = session;
                        Message(newID, MR_BROKER, MT_INIT).send(transport);
                        SafeWrite(L"session", newID, L"created, name:", m.data);
                        break;
                    }
                    case MT_EXIT:
                    {
                        std::lock_guard<std::mutex> lg(mx);
                        sessions.erase(m.header.from);
                        Message(m.header.from, MR_BROKER, MT_CONFIRM).send(transport);
                        SafeWrite(L"session", m.header.from, L"closed");
                        return;
                    }
                    case MT_GETDATA:
                    {
                        Message reply;
                        reply.receive(SRBroker());
                        reply.send(transport);
                        break;
                    }
                    default:
                    {
                        m.send(SRBroker());
                        Message(m.header.from, MR_BROKER, MT_CONFIRM).send(transport);
                        break;
                    }
                }
            }
            catch (std::exception&)
            {
                break;
            }
        }
    }
};

int main()
{
    setlocale(LC_ALL, "Russian");
    SafeWrite(L"Message Broker Server started on port 12345");

    try
    {
        int port = 12345;
        boost::asio::io_context io;
        tcp::acceptor a(io, tcp::endpoint(tcp::v4(), port));

        while (true)
        {
            std::thread(SRBroker::worker, a.accept()).detach();
        }
    }
    catch (std::exception& e)
    {
        std::cerr << "Server error: " << e.what() << std::endl;
    }

    return 0;
}