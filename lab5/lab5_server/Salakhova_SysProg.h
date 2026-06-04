#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0A00
#endif

#include <boost/asio.hpp>
#include <iostream>
#include <string>
#include <mutex>
#include <thread>
#include <map>
#include <memory>
#include <vector>

using namespace boost::asio;
using boost::asio::ip::tcp;

inline void sendData(tcp::socket& s, const void* data, size_t size)
{
    boost::system::error_code ec;
    write(s, buffer(data, size), ec);
    if (ec) throw boost::system::system_error(ec);
}

inline void receiveData(tcp::socket& s, void* data, size_t size)
{
    boost::system::error_code ec;
    read(s, buffer(data, size), ec);
    if (ec) throw boost::system::system_error(ec);
}

static std::mutex console_mx;

template<typename... Args>
inline void SafeWrite(Args... args)
{
    std::lock_guard<std::mutex> lock(console_mx);
    ((std::wcout << args << L' '), ...) << std::endl;
}