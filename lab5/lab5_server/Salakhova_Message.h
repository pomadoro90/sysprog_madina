#pragma once
#include <string>
#include "Salakhova_Interfaces.h"

struct MessageHeader
{
    int messageType;
    int size;
    int to;
    int from;
};

struct Message
{
    MessageHeader header = { 0 };
    std::wstring data;

    Message() = default;
    Message(MessageTypes messageType, const std::wstring& data = L"");
    Message(int to, MessageTypes messageType, const std::wstring& data = L"");
    Message(int to, int from, MessageTypes messageType, const std::wstring& data = L"");

    void send(const Sender& transport);
    void receive(const Receiver& transport);

    static void sendMessage(const Sender& transport, MessageTypes messageType, const std::wstring& data = L"");
    static void sendMessage(const Sender& transport, int to, MessageTypes messageType, const std::wstring& data = L"");
    static Message receiveMessage(const Receiver& transport);
};