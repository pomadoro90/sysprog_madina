#pragma once
#include <queue>
#include <mutex>
#include <condition_variable>
#include "Salakhova_Message.h"

class Session
{
    std::queue<Message> messages;
    std::mutex mtx;
    std::condition_variable cv;

public:
    int sessionID;
    std::wstring name;

    Session(int sessionID, std::wstring name = L"");
    ~Session();

    void addMessage(Message& m);
    bool getMessage(Message& m);
    bool tryGetMessage(Message& m);
};