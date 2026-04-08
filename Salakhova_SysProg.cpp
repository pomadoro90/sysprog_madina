#include "Salakhova_SysProg.h"

enum MessageTypes
{
    //MT_DATA,  <-- в первой лабе не нужна работа с Send, на будущее
    MT_CLOSE
};

struct MessageHeader
{
    int MessageType;
    int size;
};

struct Message
{
    MessageHeader header = { 0 };
    string data;

    Message() = default;
    Message(MessageTypes messageType, const string& data = "")
        :data(data)
    {
        header = { messageType, int(data.length()) };
    }
};

class SessionSalakhova
{
    queue<Message> messages;    // <-- очередь сообщений (пока что только типа MT_CLOSE)    -- требование
    CRITICAL_SECTION cs;    // <-- объект "критическая секция" для защиты каждой очереди    -- требование
    HANDLE hEvent;  // <-- объект "событие" для уведомления о сообщениях                    -- требование

public:

    int sessionID;  // <-- идентификатор сессии     -- требование
    SessionSalakhova(int sessionID)
        :sessionID(sessionID)
    {
        InitializeCriticalSection(&cs);
        hEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    }

    ~SessionSalakhova()
    {
        DeleteCriticalSection(&cs);
        CloseHandle(hEvent);
    }

    void addMessage(Message& m)
    {
        EnterCriticalSection(&cs);
        messages.push(m);
        SetEvent(hEvent);
        LeaveCriticalSection(&cs);
    }

    bool getMessage(Message& m)
    {
        bool res = false;
        WaitForSingleObject(hEvent, INFINITE);
        EnterCriticalSection(&cs);

        if (!messages.empty())
        {
            res = true;
            m = messages.front();
            messages.pop();
        }

        if (messages.empty())
        {
            ResetEvent(hEvent);
        }

        LeaveCriticalSection(&cs);
        return res;
    }

    void addMessage(MessageTypes messageType, const string& data = "")
    {
        Message m(messageType, data);
        addMessage(m);
    }
};

DWORD WINAPI MyThread(LPVOID lpParam)
{
    auto session = static_cast<SessionSalakhova*>(lpParam);
    SafeWrite("session", session->sessionID, "created");
    
    while (true)
    {
        Message m;
        if (session->getMessage(m))
        {
            switch (m.header.MessageType)
            {
            case MT_CLOSE:
                SafeWrite("session", session->sessionID, "closed");
                delete session;
                return 0;
            }
        }
    }
    return 0;
}

int main()
{
    vector<SessionSalakhova*> sessions;
    HANDLE hQuitEvent = CreateEvent(NULL, FALSE, FALSE, L"QuitEvent");

    HANDLE hStartEvent = CreateEvent(NULL, FALSE, FALSE, L"StartEvent");
    HANDLE hStopEvent = CreateEvent(NULL, FALSE, FALSE, L"StopEvent");
    HANDLE hConfirmEvent = CreateEvent(NULL, FALSE, FALSE, L"ConfirmEvent");
    HANDLE hControlEvents[2] = { hStartEvent, hStopEvent };
    
    while (true)
    {
        int actionEvent = WaitForMultipleObjects(2, hControlEvents, FALSE, INFINITE) - WAIT_OBJECT_0;
        switch (actionEvent)
        {
        case 0:
        {
            sessions.push_back(new SessionSalakhova(sessions.size()));
            CreateThread(NULL, 0, MyThread, (LPVOID)sessions.back(), 0, NULL);
            //Sleep(300);
            SetEvent(hConfirmEvent);
            //SafeWrite("creation done");
        }
        break;

        case 1:
        {
            if (!sessions.empty())
            {
                SessionSalakhova* lastSession = sessions.back();
                lastSession->addMessage(MT_CLOSE);
                //Sleep(500);
                //delete lastSession;
                sessions.pop_back();

                SetEvent(hConfirmEvent);
            }
            
            if (sessions.empty())
            {
                SetEvent(hConfirmEvent);
                return 0;
            }
            //SafeWrite("closing done");
        }
        break;
        }

        if (WaitForSingleObject(hQuitEvent, 0) == WAIT_OBJECT_0)
        {
            cout << "Shutting down..." << endl;
            SetEvent(hConfirmEvent);
            return 0;
        }
    }

    return 0;
}