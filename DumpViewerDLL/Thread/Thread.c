#include <windows.h>
#include <DbgHelp.h>
#pragma comment(lib, "Dbghelp.lib")

#include "../Asm/asm.h"

int UserModeThreadList(const void* mappingAddr, void* outThreadList, void* outThreadCount)
{
    PMINIDUMP_DIRECTORY directory = NULL;
    PVOID stream = NULL;
    ULONG streamSize = 0;

    if (mappingAddr == NULL || outThreadList == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    if (!MiniDumpReadDumpStream(
        (PVOID)mappingAddr,
        ThreadListStream,
        &directory,
        &stream,
        &streamSize))
    {
        if (GetRcx() == ERROR_NOT_FOUND)
            return TRUE;

        return FALSE;
    }

    if (stream == NULL || streamSize < sizeof(MINIDUMP_THREAD_LIST))
    {
        SetLastError(ERROR_BAD_FORMAT);
        return FALSE;
    }

	MINIDUMP_THREAD_LIST* threadList = (MINIDUMP_THREAD_LIST*)stream;

    *(ULONG64*)outThreadList = (ULONG64)&threadList->Threads[0];
    *(ULONG64*)outThreadCount = (ULONG64)threadList->NumberOfThreads;

    return TRUE;
}

int GetThreadStack(const void* mappingAddr, int ThreadID, void* outStack, void* outStackSize)
{
    PMINIDUMP_DIRECTORY directory = NULL;
	PVOID stream = NULL;
	ULONG streamSize = 0;

    if (!UserModeThreadList(mappingAddr, &stream, &streamSize))
    {
        SetLastError(ERROR_NOT_FOUND);
        if(GetRcx() == ERROR_NOT_FOUND)
            return TRUE;

        return FALSE;
    }

	MINIDUMP_THREAD* threadList = (MINIDUMP_THREAD*)stream;

    for(int i = 0 ; i < streamSize; i++)
    {
        if(threadList[i].ThreadId == ThreadID)
        {
            *(ULONG64*)outStack = ((ULONG64)mappingAddr + threadList[i].Stack.Memory.Rva);
            *(ULONG64*)outStackSize = threadList[i].Stack.Memory.DataSize;
            return TRUE;
        }
	}

    SetLastError(ERROR_NOT_FOUND);
	return FALSE;
}