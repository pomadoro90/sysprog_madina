#pragma once

#include "Salakhova_SysProg.h"
#include "Salakhova_enums.h"

struct Message;

class Sender
{
public:
    virtual void send(Message&) const = 0;
};

class Receiver
{
public:
    virtual void receive(Message&) const = 0;
};

struct MessageHeader
{
    int type;
    int size;
    int to;
    int from;
};

struct Message
{
    MessageHeader header = { 0 };
    std::wstring data;

    Message() = default;

    Message(MessageTypes messageType, const std::wstring& data = L"")
        : data(data)
    {
        header = { messageType, int(data.length() * sizeof(wchar_t)) };
    }

    Message(int to, MessageTypes messageType, const std::wstring& data = L"")
        : data(data)
    {
        header = { messageType, int(data.length() * sizeof(wchar_t)), to };
    }

    Message(int to, int from, MessageTypes messageType, const std::wstring& data = L"")
        : data(data)
    {
        header = { messageType, int(data.length() * sizeof(wchar_t)), to, from };
    }

    Message& send(const Sender& sender)
    {
        sender.send(*this);
        return *this;
    }

    Message& receive(const Receiver& receiver)
    {
        receiver.receive(*this);
        return *this;
    }

    static Message sendMessage(const Sender& sender, MessageTypes messageType, const std::wstring& data = L"")
    {
        Message m(messageType, data);
        m.send(sender);
        return m;
    }

    static Message sendMessage(const Sender& sender, int to, MessageTypes messageType, const std::wstring& data = L"")
    {
        Message m(to, messageType, data);
        m.send(sender);
        return m;
    }

    static Message receiveMessage(const Receiver& receiver)
    {
        Message m;
        m.receive(receiver);
        return m;
    }
};

class SRSocket : public Sender, public Receiver
{
    boost::asio::io_context _io;
    tcp::socket _s;
    tcp::resolver _r;
    tcp::socket& s;
    static inline int clientID = 0;
    bool client = false;

public:
    SRSocket(bool client = false)
        : _s(_io), _r(_io), s(_s), client(client)
    {
        boost::asio::connect(_s, _r.resolve("127.0.0.1", "12345"));
    }

    SRSocket(tcp::socket& s, bool client = false)
        : _s(_io), _r(_io), s(s), client(client)
    {
    }

    virtual void send(Message& m) const override
    {
        if (client)
            m.header.from = clientID;

        sendData(s, &m.header);
        if (m.header.size)
        {
            sendData(s, m.data.data(), m.header.size);
        }

        if (client)
            receive(m);
    }

    virtual void receive(Message& m) const override
    {
        receiveData(s, &m.header);
        if (m.header.size)
        {
            m.data.resize(m.header.size / sizeof(wchar_t));
            receiveData(s, m.data.data(), m.header.size);
        }
        if (client && m.header.type == MT_INIT)
        {
            clientID = m.header.to;
        }
    }
};