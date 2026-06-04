#pragma once

enum MessageTypes
{
    MT_INIT = 0,
    MT_EXIT = 1,
    MT_GETDATA = 2,
    MT_DATA = 3,
    MT_NODATA = 4,
    MT_CONFIRM = 5
};

enum MessageRecipients
{
    MR_BROKER = 10,
    MR_ALL = 50,
    MR_USER = 100
};

struct Message;

class Sender
{
public:
    virtual void send(Message&) const = 0;
    virtual ~Sender() = default;
};

class Receiver
{
public:
    virtual void receive(Message&) const = 0;
    virtual ~Receiver() = default;
};