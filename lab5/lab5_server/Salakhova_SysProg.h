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

template<typename T>
void sendData(tcp::socket& s, const T* data, size_t count = 1)
{
    boost::system::error_code ec;
    write(s, buffer(data, sizeof(T) * count), ec);
    if (ec) throw boost::system::system_error(ec);
}

template<typename T>
void receiveData(tcp::socket& s, T* data, size_t count = 1)
{
    boost::system::error_code ec;
    read(s, buffer(data, sizeof(T) * count), ec);
    if (ec) throw boost::system::system_error(ec);
}

static std::mutex console_mx;

template<typename... Args>
inline void SafeWrite(Args... args)
{
    std::lock_guard<std::mutex> lock(console_mx);
    ((std::cout << args << ' '), ...) << std::endl;
}