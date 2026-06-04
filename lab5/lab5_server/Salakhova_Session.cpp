#include "Salakhova_Session.h"

Session::Session(int sessionID, std::wstring name)
    : sessionID(sessionID), name(name)
{
}

Session::~Session()
{
}

void Session::addMessage(Message& m)
{
    {
        std::lock_guard<std::mutex> lock(mtx);
        messages.push(m);
    }
    cv.notify_one();
}

bool Session::getMessage(Message& m)
{
    std::unique_lock<std::mutex> lock(mtx);
    cv.wait(lock, [this]() { return !messages.empty(); });
    if (!messages.empty())
    {
        m = messages.front();
        messages.pop();
        return true;
    }
    return false;
}

bool Session::tryGetMessage(Message& m)
{
    std::lock_guard<std::mutex> lock(mtx);
    if (messages.empty()) return false;
    m = messages.front();
    messages.pop();
    return true;
}