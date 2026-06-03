#pragma once

#include "Message.h"
#include <queue>
#include <mutex>

class Session
{
    std::queue<Message> messages;
    std::mutex mx;

public:
    int sessionID;
    std::wstring name;

    Session(int sessionID, std::wstring name = L"")
        : sessionID(sessionID), name(name)
    {
    }

    void addMessage(Message& m)
    {
        std::lock_guard<std::mutex> lg(mx);
        messages.push(m);
    }

    bool getMessage(Message& m)
    {
        std::lock_guard<std::mutex> lg(mx);
        if (messages.empty())
            return false;
        m = messages.front();
        messages.pop();
        return true;
    }
};