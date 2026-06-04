#include "Salakhova_Message.h"

Message::Message(MessageTypes messageType, const std::wstring& data)
    : data(data)
{
    header = { messageType, int(data.length() * sizeof(wchar_t)) };
}

Message::Message(int to, MessageTypes messageType, const std::wstring& data)
    : data(data)
{
    header = { messageType, int(data.length() * sizeof(wchar_t)), to };
}

Message::Message(int to, int from, MessageTypes messageType, const std::wstring& data)
    : data(data)
{
    header = { messageType, int(data.length() * sizeof(wchar_t)), to, from };
}

void Message::send(const Sender& transport)
{
    transport.send(*this);
}

void Message::receive(const Receiver& transport)
{
    transport.receive(*this);
}

void Message::sendMessage(const Sender& transport, MessageTypes messageType, const std::wstring& data)
{
    Message m(messageType, data);
    m.send(transport);
}

void Message::sendMessage(const Sender& transport, int to, MessageTypes messageType, const std::wstring& data)
{
    Message m(to, messageType, data);
    m.send(transport);
}

Message Message::receiveMessage(const Receiver& transport)
{
    Message m;
    m.receive(transport);
    return m;
}