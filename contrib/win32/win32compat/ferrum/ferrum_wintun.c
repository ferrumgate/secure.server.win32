

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
    ferrum_client = malloc(sizeof(ferrum_client_t));
    ZeroMemory(ferrum_client, sizeof(ferrum_client_t));
    HMODULE Wintun = InitializeWintun();
    if (!Wintun)
        return LogError(L"failed to initialize wintun", GetLastError());
    ferrum_client->loadedLib = 1;
    //WintunSetLogger(ConsoleLogger);
    Log(WINTUN_LOG_INFO, L"wintun library loaded");

    DWORD LastError = ERROR_SUCCESS;


    // if (!SetConsoleCtrlHandler(CtrlHandler, TRUE))
    // {
    //     LastError = LogError("failed to set console handler", GetLastError());
    //     goto cleanupQuit;
    // }

    GUID guid = { 0x43dad8f2, 0x3304, 0x4033, { 0x8a, 0x6a, 0xb9, 0x47, 0x0c, 0x10, 0xc5, 0x75 } };


    WINTUN_ADAPTER_HANDLE Adapter = WintunCreateAdapter(L"FerrumGate", L"Secure", &guid);
    if (!Adapter)
    {
        LastError = GetLastError();
        LogError(L"failed to create adapter", LastError);
        goto cleanupQuit;

    }


    DWORD Version = WintunGetRunningDriverVersion();
    Log(WINTUN_LOG_INFO, L"wintun v%u.%u loaded", (Version >> 16) & 0xff, (Version >> 0) & 0xff);

    ferrum_client->wintun = Wintun;
    ferrum_client->adapter = Adapter;
    ferrum_client->initted = 1;
    WINTUN_SESSION_HANDLE Session = WintunStartSession(ferrum_client->adapter, 0x400000);
    if (!Session)
    {
        LastError = LogLastError(L"Failed to create tun session");
        return LastError;
    }
    ferrum_client->session = Session;
    ferrum_client->session_event = WintunGetReadWaitEvent(Session);
    return LastError;
cleanupAdapter:
    WintunCloseAdapter(Adapter);
cleanupQuit:
    SetConsoleCtrlHandler(CtrlHandler, FALSE);
    // CloseHandle(ferrum_client->quitEvent);
cleanupWintun:
    FreeLibrary(Wintun);
    return LastError;
}

int __cdecl
FerrumStopWinTun() {
    if (ferrum_client->initted) {
        //SetEvent(ferrum_client->quitEvent);
        if (ferrum_client->session)
            WintunEndSession(ferrum_client->session);
        WintunCloseAdapter(ferrum_client->adapter);
        //SetConsoleCtrlHandler(CtrlHandler, FALSE);
        //CloseHandle(ferrum_client->quitEvent);

    }
    if (ferrum_client->loadedLib)
        FreeLibrary(ferrum_client->wintun);
    ZeroMemory(&ferrum_client, sizeof(ferrum_client));

    return ERROR_SUCCESS;

}

VOID CALLBACK TimerRoutine(PVOID lpParam, BOOLEAN TimerOrWaitFired)
{


    SetEvent(ferrum_client->timer1_event);

}

void FollowTun(void* param) {
    HANDLE tun = WintunGetReadWaitEvent(ferrum_client->session);
    HANDLE timer;
    HANDLE hTimerQueue = NULL;
    hTimerQueue = CreateTimerQueue();
    if (NULL == hTimerQueue)
    {
        fprintf(stderr, "CreateTimerQueue failed (%d)\n", GetLastError());
        return;
    }
    CreateTimerQueueTimer(&timer, hTimerQueue, TimerRoutine, NULL, 1000, 1, 0);
    ferrum_client->timer1_event = CreateEvent(NULL, TRUE, FALSE, NULL);
    HANDLE events[] = { tun,ferrum_client->timer1_event };

    //struct pollfd fds[1];
    int counter = 0;
    char tmp[128];
    while (ferrum_client->work)
    {
       // fprintf(stderr, "signaling tun\n");
        ResetEvent(ferrum_client->timer1_event);
        WaitForMultipleObjects(_countof(events), events, FALSE, INFINITE);
        int written = w32_write(ferrum_client->sync.socket[1], &counter, 1);
        
        if (written < 0)
        {
            fprintf(stderr, "sync write problem\n");
            return;

        }

    }
}




int FerrumRxTxWinTun(void) {

    DWORD  LastError = ERROR_SUCCESS;

    ferrum_client->work = TRUE;


    Log(WINTUN_LOG_INFO, L"starting windows tun");


    int result;
    //result=pipe(ferrum_client->pipes.write);
    result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->pipes.write);
    if (result == -1) {
        fprintf(stderr, "create write pipes failed %s\n", strerror(errno));
        return 1;
    }
    
    set_nonblock(ferrum_client->pipes.write[0]);
    set_nonblock(ferrum_client->pipes.write[1]);
    //result=pipe(ferrum_client->pipes.read);
    result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->pipes.read);
    if (result == -1) {
        fprintf(stderr, "create read pipes failed %s\n", strerror(errno));
        return 1;
    }
    set_nonblock(ferrum_client->pipes.read[0]);
    set_nonblock(ferrum_client->pipes.read[1]);

    result = socketpair(AF_UNIX, SOCK_STREAM, 0, ferrum_client->sync.socket);
    //result=pipe(ferrum_client->sync.socket);
    if (result == -1) {
        fprintf(stderr, "create sync socket failed %s\n", strerror(errno));
        return 1;
    }
    set_nonblock(ferrum_client->sync.socket[0]);
    set_nonblock(ferrum_client->sync.socket[1]);
    
    CreateThread(NULL,0,FollowTun,NULL,0,NULL);
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









