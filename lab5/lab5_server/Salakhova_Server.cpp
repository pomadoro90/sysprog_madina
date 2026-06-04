#include "Salakhova_Message.h"
#include "Salakhova_Session.h"

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
        if (iSessionFrom != sessions.end())
        {
            auto iSessionTo = sessions.find(m.header.to);
            if (iSessionTo != sessions.end())
            {
                iSessionTo->second->addMessage(m);
            }
            else if (m.header.to == MR_ALL)
            {
                for (auto& [id, session] : sessions)
                {
                    if (id != m.header.from)
                        session->addMessage(m);
                }
            }
        }
    }

    virtual void receive(Message& m) const override
    {
        std::lock_guard<std::mutex> lg(mx);
        auto iSession = sessions.find(m.header.from);
        if (iSession == sessions.end() || !iSession->second->getMessage(m))
            m = { m.header.from, MessageRecipients::MR_BROKER, MessageTypes::MT_NODATA };
    }

    static void worker(tcp::socket s)
    {
        while (true)
        {
            try
            {
                Message m = Message::receiveMessage(SRSocket(s));
                SafeWrite("msg: to=", m.header.to, "from=", m.header.from, "type=", m.header.type);

                switch (m.header.type)
                {
                    case MT_INIT:
                    {
                        std::lock_guard<std::mutex> lg(mx);
                        auto session = std::make_shared<Session>(++maxID, m.data);
                        sessions[session->sessionID] = session;
                        Message(session->sessionID, MR_BROKER, MT_INIT).send(SRSocket(s));
                        break;
                    }
                    case MT_EXIT:
                    {
                        std::lock_guard<std::mutex> lg(mx);
                        sessions.erase(m.header.from);
                        Message(m.header.from, MR_BROKER, MT_CONFIRM).send(SRSocket(s));
                        return;
                    }
                    case MT_GETDATA:
                    {
                        m.receive(SRBroker()).send(SRSocket(s));
                        break;
                    }
                    default:
                    {
                        m.send(SRBroker());
                        Message(m.header.from, MR_BROKER, MT_CONFIRM).send(SRSocket(s));
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
    SafeWrite("Message Broker Server started on port 12345");

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