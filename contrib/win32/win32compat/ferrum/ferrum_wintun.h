#pragma once

#ifndef __FERRUM_WINTUN_H__
#define __FERRUM_WINTUN_H__

#ifdef __cplusplus
extern "C" {
#endif


#include <winsock2.h>
#include "w32fd.h"
#include <sys/socket.h>
#include <ws2ipdef.h>
#include <iphlpapi.h>
#include <mstcpip.h>
#include <ip2string.h>
#include <winternl.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include "wintun.h"






	// starts wintun adapter
	int FerrumStartWinTun(void);
	// stops wintun adapter
	int FerrumStopWinTun(void);
	// start pipes for rx tx
	int FerrumRxTxWinTun(void);
	// read from tun device
	int FerrumReadWinTun(char** buffer, size_t* readed);
	// free readed buffer
	int FerrumWinTunReadFree(char* buffer);
	// write to tun device
	int FerrumWriteWinTun(char* buffer, size_t buf_len);



	typedef struct {
		// loaded wintun.dll
		int loadedLib;
		// is initted wintun adapter
		int initted;
		// adapter starter successfully
		int work;
		// wintun handle
		HMODULE wintun;
		// wintun adapter
		WINTUN_ADAPTER_HANDLE adapter;
		// wintun session
		WINTUN_SESSION_HANDLE session;
		//HANDLE quitEvent;
		HANDLE timer1_event;
		HANDLE session_event;
		struct {
			int read[2];
			int write[2];
		}pipes;

		struct {
			int socket[2];
		}sync;





	}ferrum_client_t;

	extern ferrum_client_t* ferrum_client;


#ifdef __cplusplus
}
#endif

#endif