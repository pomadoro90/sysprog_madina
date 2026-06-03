#pragma once

enum MessageTypes
{
    MT_INIT,
    MT_EXIT,
    MT_GETDATA,
    MT_DATA,
    MT_NODATA,
    MT_CONFIRM
};

enum MessageRecipients
{
    MR_BROKER = 10,
    MR_ALL = 50,
    MR_USER = 100
};