

/* SPDX-License-Identifier: GPL-2.0
 *
 * Copyright (C) 2018-2021 WireGuard LLC. All Rights Reserved.
 */

#include "ferrum_wintun.h"
 // global ferrum object for states
ferrum_client_t* ferrum_client;

static WINTUN_CREATE_ADAPTER_FUNC* WintunCreateAdapter;
static WINTUN_CLOSE_ADAPTER_FUNC* WintunCloseAdapter;
static WINTUN_OPEN_ADAPTER_FUNC* WintunOpenAdapter;
static WINTUN_GET_ADAPTER_LUID_FUNC* WintunGetAdapterLUID;
static WINTUN_GET_RUNNING_DRIVER_VERSION_FUNC* WintunGetRunningDriverVersion;
static WINTUN_DELETE_DRIVER_FUNC* WintunDeleteDriver;
static WINTUN_SET_LOGGER_FUNC* WintunSetLogger;
static WINTUN_START_SESSION_FUNC* WintunStartSession;
static WINTUN_END_SESSION_FUNC* WintunEndSession;
static WINTUN_GET_READ_WAIT_EVENT_FUNC* WintunGetReadWaitEvent;
static WINTUN_RECEIVE_PACKET_FUNC* WintunReceivePacket;
static WINTUN_RELEASE_RECEIVE_PACKET_FUNC* WintunReleaseReceivePacket;
static WINTUN_ALLOCATE_SEND_PACKET_FUNC* WintunAllocateSendPacket;
static WINTUN_SEND_PACKET_FUNC* WintunSendPacket;




static HMODULE
InitializeWintun(void)
{
    HMODULE Wintun =
        LoadLibraryExW(L"wintun.dll", NULL, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!Wintun)
        return NULL;
#define X(Name) ((*(FARPROC *)&Name = GetProcAddress(Wintun, #Name)) == NULL)
    if (X(WintunCreateAdapter) || X(WintunCloseAdapter) || X(WintunOpenAdapter) || X(WintunGetAdapterLUID) ||
        X(WintunGetRunningDriverVersion) || X(WintunDeleteDriver) || X(WintunSetLogger) || X(WintunStartSession) ||
        X(WintunEndSession) || X(WintunGetReadWaitEvent) || X(WintunReceivePacket) || X(WintunReleaseReceivePacket) ||
        X(WintunAllocateSendPacket) || X(WintunSendPacket))
#undef X
    {
        DWORD LastError = GetLastError();
        FreeLibrary(Wintun);
        SetLastError(LastError);
        return NULL;
    }
    return Wintun;
}

static void CALLBACK
ConsoleLogger(_In_ WINTUN_LOGGER_LEVEL Level, _In_ DWORD64 Timestamp, _In_z_ const WCHAR* LogLine)
{
    SYSTEMTIME SystemTime;
    FileTimeToSystemTime((FILETIME*)&Timestamp, &SystemTime);
    WCHAR LevelMarker;
    switch (Level)
    {
    case WINTUN_LOG_INFO:
        LevelMarker = L'+';
        break;
    case WINTUN_LOG_WARN:
        LevelMarker = L'-';
        break;
    case WINTUN_LOG_ERR:
        LevelMarker = L'!';
        break;
    default:
        return;
    }
    fwprintf(
        stderr,
        L"%04u-%02u-%02u %02u:%02u:%02u.%04u [%c] %s\n",
        SystemTime.wYear,
        SystemTime.wMonth,
        SystemTime.wDay,
        SystemTime.wHour,
        SystemTime.wMinute,
        SystemTime.wSecond,
        SystemTime.wMilliseconds,
        LevelMarker,
        LogLine);
}

static DWORD64 Now(VOID)
{
    LARGE_INTEGER Timestamp;
    NtQuerySystemTime(&Timestamp);
    return Timestamp.QuadPart;
}

static DWORD
LogError(_In_z_ const WCHAR* Prefix, _In_ DWORD Error)
{
    WCHAR* SystemMessage = NULL, * FormattedMessage = NULL;
    FormatMessageW(
        FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_MAX_WIDTH_MASK,
        NULL,
        HRESULT_FROM_SETUPAPI(Error),
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (void*)&SystemMessage,
        0,
        NULL);
    FormatMessageW(
        FORMAT_MESSAGE_FROM_STRING | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_ARGUMENT_ARRAY |
        FORMAT_MESSAGE_MAX_WIDTH_MASK,
        SystemMessage ? L"%1: %3(Code 0x%2!08X!)" : L"%1: Code 0x%2!08X!",
        0,
        0,
        (void*)&FormattedMessage,
        0,
        (va_list*)(DWORD_PTR[]) { (DWORD_PTR)Prefix, (DWORD_PTR)Error, (DWORD_PTR)SystemMessage });
    if (FormattedMessage)
        ConsoleLogger(WINTUN_LOG_ERR, Now(), FormattedMessage);
    LocalFree(FormattedMessage);
    LocalFree(SystemMessage);
    return Error;
}

static DWORD
LogLastError(_In_z_ const WCHAR* Prefix)
{
    DWORD LastError = GetLastError();
    LogError(Prefix, LastError);
    SetLastError(LastError);
    return LastError;
}

static void
Log(_In_ WINTUN_LOGGER_LEVEL Level, _In_z_ const WCHAR* Format, ...)
{
    WCHAR LogLine[0x200];
    va_list args;
    va_start(args, Format);
    _vsnwprintf_s(LogLine, _countof(LogLine), _TRUNCATE, Format, args);
    va_end(args);
    ConsoleLogger(Level, Now(), LogLine);
}




static BOOL WINAPI
CtrlHandler(_In_ DWORD CtrlType)
{
    switch (CtrlType)
    {
    case CTRL_C_EVENT:
    case CTRL_BREAK_EVENT:
    case CTRL_CLOSE_EVENT:
    case CTRL_LOGOFF_EVENT:
    case CTRL_SHUTDOWN_EVENT:
        Log(WINTUN_LOG_INFO, L"cleaning up and shutting down...");
        return TRUE;
    }
    return FALSE;
}

static void
PrintPacket(_In_ const BYTE* Packet, _In_ DWORD PacketSize)
{
    if (PacketSize < 20)
    {
        Log(WINTUN_LOG_INFO, L"received packet without room for an IP header");
        return;
    }
    BYTE IpVersion = Packet[0] >> 4, Proto;
    CHAR Src[46] = { 0 }, Dst[46] = { 0 };
    if (IpVersion == 4)
    {
        inet_ntop(AF_INET, (struct in_addr*)&Packet[12], Src, INET_ADDRSTRLEN);
        inet_ntop(AF_INET, (struct in_addr*)&Packet[16], Dst, INET_ADDRSTRLEN);
        Proto = Packet[9];
        Packet += 20, PacketSize -= 20;
    }
    else if (IpVersion == 6 && PacketSize < 40)
    {
        Log(WINTUN_LOG_INFO, L"received packet without room for an IP header");
        return;
    }
    else if (IpVersion == 6)
    {
        inet_ntop(AF_INET6, (struct in6_addr*)&Packet[8], Src, INET6_ADDRSTRLEN);
        inet_ntop(AF_INET6, (struct in6_addr*)&Packet[24], Dst, INET6_ADDRSTRLEN);
        Proto = Packet[6];
        Packet += 40, PacketSize -= 40;
    }
    else
    {
        Log(WINTUN_LOG_INFO, L"received packet that was not IP");
        return;
    }
    if (Proto == 1 && PacketSize >= 8 && Packet[0] == 0)
        Log(WINTUN_LOG_INFO, L"received IPv%d ICMP echo reply from %s to %s", IpVersion, Src, Dst);
    else
        Log(WINTUN_LOG_INFO, L"received IPv%d proto 0x%x packet from %s to %s", IpVersion, Proto, Src, Dst);
}





int __cdecl
FerrumStartWinTun()
{
    //clear all states
    DWORD LastError = ERROR_SUCCESS;
    int tryCount = 4;
    HMODULE Wintun;
    WINTUN_ADAPTER_HANDLE Adapter;
    while (tryCount--) {
        ferrum_client = malloc(sizeof(ferrum_client_t));
        ZeroMemory(ferrum_client, sizeof(ferrum_client_t));
        Wintun = InitializeWintun();
        if (!Wintun)
            return LogError(L"failed to initialize wintun", GetLastError());
        ferrum_client->loadedLib = 1;
        //WintunSetLogger(ConsoleLogger);
        Log(WINTUN_LOG_INFO, L"wintun library loaded");

        GUID guid = { 0x43dad8f2, 0x3304, 0x4033, { 0x8a, 0x6a, 0xb9, 0x47, 0x0c, 0x10, 0xc5, 0x75 } };


        Adapter = WintunCreateAdapter(L"FerrumGate", L"Secure", &guid);
        if (!Adapter)
        {
            LastError = GetLastError();
            LogError(L"failed to create adapter", LastError);
            if (!tryCount)//try finished
            {
                fprintf(stderr, "try one more loading wintun\n");
                FreeLibrary(Wintun);
                return LastError;
            }
            else {
                FreeLibrary(Wintun);
                continue;
            }

        }



        DWORD Version = WintunGetRunningDriverVersion();
        Log(WINTUN_LOG_INFO, L"wintun v%u.%u loaded", (Version >> 16) & 0xff, (Version >> 0) & 0xff);

        ferrum_client->wintun = Wintun;
        ferrum_client->adapter = Adapter;
        ferrum_client->initted = 1;
        break;
    }
    WINTUN_SESSION_HANDLE Session = WintunStartSession(ferrum_client->adapter, 0x400000);
    if (!Session)
    {
        LastError = LogLastError(L"Failed to create tun session");
        WintunCloseAdapter(Adapter);
        FreeLibrary(Wintun);
        return LastError;
    }
    ferrum_client->session = Session;
    ferrum_client->session_event = WintunGetReadWaitEvent(Session);
    return 0;
}

int __cdecl
FerrumStopWinTun() {
    if (ferrum_client->initted) {
        
        if (ferrum_client->session)
            WintunEndSession(ferrum_client->session);
        WintunCloseAdapter(ferrum_client->adapter);

    }
    if (ferrum_client->loadedLib)
        FreeLibrary(ferrum_client->wintun);
    ZeroMemory(&ferrum_client, sizeof(ferrum_client));

    return ERROR_SUCCESS;

}

// for perfornmance testing used
static int SocketListen(int pair[4]) {
    struct sockaddr_in addr;
    int len = sizeof(addr);
    int result= w32_accept(pair[3],(struct sockaddr*) & addr, &len);
    if (result == SOCKET_ERROR) {
        fprintf(stderr, "socket server  accept bind failed\n");
        return -1;
    }
    pair[1] = result;
    fprintf(stderr, "client connected\n");
    return 0;
}
//for performance testing used
static int CreateSocketPair(int pair[4]) {
     int listener=w32_socket(AF_INET, SOCK_STREAM, 0);
     if (listener == INVALID_SOCKET) {
         fprintf(stderr, "socket server create failed\n");
         return -1;
     }
     struct sockaddr_in addr;
     addr.sin_addr.s_addr = inet_addr("127.0.0.1");
     addr.sin_family = AF_INET;
     addr.sin_port = 0;
     int result=w32_bind(listener, (struct sockaddr *) & addr, sizeof(addr));
     if (result == SOCKET_ERROR) {
         fprintf(stderr, "socket server  bind failed\n");
         return -1;
     }
     int len = sizeof(addr);
     w32_getsockname(listener, (struct sockaddr*)&addr, &len);
     fprintf(stderr, "socket server listening on %d\n",ntohs(addr.sin_port));
     result=w32_listen(listener, 10);
     if (result == SOCKET_ERROR) {
         fprintf(stderr, "socket server listen failed\n");
         return -1;
     }
     pair[3] = listener;
     CreateThread(NULL, 0, SocketListen, pair, 0, NULL);
     Sleep(1000);
     int client = w32_socket(AF_INET, SOCK_STREAM, 0);
     if (client == INVALID_SOCKET) {
         fprintf(stderr, "socket client create failed\n");
         return -1;
     }
     result=w32_connect(client, (struct sockaddr*)&addr, len);
     if (result == SOCKET_ERROR) {
         fprintf(stderr, "socket client connect failed\n");
         return -1;
     }
     pair[0] = client;
     return 0;
}




int FerrumRxTxWinTun(void) {

    DWORD  LastError = ERROR_SUCCESS;

    ferrum_client->work = TRUE;


    Log(WINTUN_LOG_INFO, L"starting windows tun");


    int result;
    //result=pipe(ferrum_client->pipes.write);
    result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->pipes.write);
   //result= CreateSocketPair(ferrum_client->pipes.write);
    if (result == -1) {
        fprintf(stderr, "create write pipes failed %s\n", strerror(errno));
        return 1;
    }
    
    set_nonblock(ferrum_client->pipes.write[0]);
    set_nonblock(ferrum_client->pipes.write[1]);
    //result=pipe(ferrum_client->pipes.read);
    result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->pipes.read);
    //result=CreateSocketPair(ferrum_client->pipes.read);
    if (result == -1) {
        fprintf(stderr, "create read pipes failed %s\n", strerror(errno));
        return 1;
    }
    set_nonblock(ferrum_client->pipes.read[0]);
    set_nonblock(ferrum_client->pipes.read[1]);

    //result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->sync.socket);
    ////result=pipe(ferrum_client->sync.socket);
    //if (result == -1) {
    //    fprintf(stderr, "create sync socket failed %s\n", strerror(errno));
    //    return 1;
    //}
    //set_nonblock(ferrum_client->sync.socket[0]);
    //set_nonblock(ferrum_client->sync.socket[1]);
    
    //CreateThread(NULL,0,FollowTun,NULL,0,NULL);
    return ERROR_SUCCESS;

}

int FerrumReadWinTun(char** buffer, size_t* readed) {
    DWORD PacketSize;
    BYTE* Packet = WintunReceivePacket(ferrum_client->session, &PacketSize);
    if (Packet)
    {
        *buffer = Packet;
        *readed = PacketSize;
        return 0;
    }
    return ERROR_NOT_READY;
}
int FerrumWinTunReadFree(char* buffer) {
    WintunReleaseReceivePacket(ferrum_client->session, buffer);
    return 0;
}
int FerrumWriteWinTun(char* buffer, size_t buf_len) {
    BYTE* Packet = WintunAllocateSendPacket(ferrum_client->session, buf_len);
    if (Packet)
    {
        memcpy(Packet, buffer, buf_len);
        WintunSendPacket(ferrum_client->session, Packet);
        return 0;
    }
    return GetLastError();
}


// which events fired connection in, connection out, tun, channel write fd
static WSAEVENT events[4]={0,0,0,0};

// create a socket event
static int createSockEvent(int sock,int defaultIndex, int index, WSAEVENT* eventArray,SOCKET *socketArray, int ev) {
    WSAEVENT evsockin;
    if (events[defaultIndex])
        evsockin = events[defaultIndex];
    else {
        evsockin = WSACreateEvent();
        if (evsockin == WSA_INVALID_EVENT) {
            fprintf(stderr, "event create error \n");
            return -1;
        }
    }
    events[defaultIndex] = evsockin;
    
    SOCKET wsockin = (SOCKET)w32_fd_to_handle(sock);
    if (wsockin == INVALID_HANDLE_VALUE) {
        fprintf(stderr, "socket conversion error\n");
        return -1;
    }
    socketArray[index] = wsockin;
    eventArray[index] = evsockin;
    int result = WSAEventSelect(wsockin, eventArray[index], ev);
    if (result == SOCKET_ERROR) {
        fprintf(stderr, "event select error\n");
        return -1;
    }
    return 0;
}


int FerrumPoll(int conin, int conout, void* tunhandle, int cwfd, int results[4], int timeoutms) {

    DWORD eventId;
    HANDLE tun = tunhandle;
    DWORD eventTotal = 0;
    DWORD result;
    WSANETWORKEVENTS networkEvents;
    WSAEVENT eventArray[WSA_MAXIMUM_WAIT_EVENTS];
    SOCKET socketArray[WSA_MAXIMUM_WAIT_EVENTS];
    int indexToDefault[WSA_MAXIMUM_WAIT_EVENTS];
   
    if (conout>-1){
        // on windows this not working as posix, because of this we will allways return POLL_OUT
        timeoutms = 0;
    }
  
    if (conin > -1) {
       
        result = createSockEvent(conin,0, eventTotal, eventArray,socketArray, FD_READ);
        if (result)return result;
        indexToDefault[eventTotal] = 0; // save which event is this, 0 event is connection in
        eventTotal++;
    }

   
    if ((int)tunhandle > -1) {
        eventArray[eventTotal] = tun;
        indexToDefault[eventTotal] = 1; //save which event is this, 1 event is tun event
        eventTotal++;
    }


    
    if (cwfd > -1) {
        // on windows this not working as posix, because of this we will allways return POLL_OUT
        timeoutms = 0;
    }

        
    eventId = WSA_WAIT_IO_COMPLETION;
    while (eventId == WSA_WAIT_IO_COMPLETION) {
        if ((eventId = WSAWaitForMultipleEvents(eventTotal, eventArray, FALSE, timeoutms, FALSE)) == WSA_WAIT_FAILED)
        {
            fprintf(stderr, "WSAWaitForMultipleEvents() failed with error %d\n", WSAGetLastError());
            return -1;
        }

        if (eventId == WSA_WAIT_IO_COMPLETION) {
                continue;
        }
        if (eventId == WSA_WAIT_TIMEOUT) {

            if (timeoutms != 0)
                return 0;
        }
    }
        
    
    if (eventId != WSA_WAIT_TIMEOUT && eventId!=WSA_WAIT_IO_COMPLETION) {
        eventId = eventId - WSA_WAIT_EVENT_0;

       // fprintf(stderr, "eventId fired :%d ", eventId);
        int defaultId = indexToDefault[eventId];
        if (defaultId == 0) {
            WSANETWORKEVENTS nevent;
            WSAEnumNetworkEvents(socketArray[eventId], eventArray[eventId], &nevent);

            if (nevent.lNetworkEvents & FD_READ) {
                results[0] = 1;//connection in is fired
                //fprintf(stderr, "read\n");
            }
            if (nevent.lNetworkEvents & FD_WRITE) {
                results[1] = 1;//save connection out is fired
               // fprintf(stderr, "write\n");
            }
        }
        if (defaultId == 1) {
            //fprintf(stderr, "tun\n");
            //ResetEvent(eventArray[eventId]);
            results[2] = 1; // tun is fired

        }
        if (defaultId == 2) {
            WSANETWORKEVENTS nevent;
            WSAEnumNetworkEvents(socketArray[eventId], eventArray[eventId], &nevent);

            if (nevent.lNetworkEvents & FD_WRITE) {
                results[3] = 1; // channel write fd is ready

            }
        }
    }
    // for posix compat, set allways fired if asked
    if (conout > -1)
        results[1] = 1;
    if (cwfd > -1)
        results[3] = 1;


    return 1;
}









