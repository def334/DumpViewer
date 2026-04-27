#pragma once

#ifdef __cplusplus
extern "C" {
#endif

#include "Internel.h"
#include <DbgHelp.h>

#define KERNEL_DUMP_HEADER64 DUMP_HEADER64
#define USER_DUMP_HEADER64 USERMODE_CRASHDUMP_HEADER64

typedef struct _DUMP_FILE_INFO
{
    wchar_t* path;
    DWORD fileAttributes;
    ULONGLONG fileSize;
    DWORD isKernelMode;
	DWORD dumpMode;             //using c# umamnager structure, not matching with DUMP_TYPE 
    FILETIME creationTime;
    FILETIME lastWriteTime;
	void* HEADER;              
    MAPPED_FILE_BUFFER mappedFile;
} DUMP_FILE_INFO, * PDUMP_FILE_INFO;

typedef struct _DUMP_CALLSTACK_FRAME
{
    ULONG32 threadId;
    ULONG32 frameIndex;
    ULONG64 instructionPointer;
    ULONG64 stackPointer;
} DUMP_CALLSTACK_FRAME;

typedef struct _DUMP_MODULE_INFO
{
    ULONG64 baseOfImage;
    ULONG32 sizeOfImage;
    ULONG32 checksum;
    ULONG32 timeDateStamp;
    wchar_t moduleName[MAX_PATH];
} DUMP_MODULE_INFO;

typedef struct _DUMP_THREAD_INFO
{
    ULONG32 threadId;
    ULONG32 suspendCount;
    ULONG32 priorityClass;
    ULONG32 priority;
    ULONG64 teb;

    ULONG64 stackStart;
    ULONG32 stackDataSize;
    ULONG32 stackRva;

    ULONG32 contextDataSize;
    ULONG32 contextRva;
} DUMP_THREAD_INFO;

__declspec(dllexport) BOOL OpenDumpFile(
    const wchar_t* dumpFilePath,
    FILE_OPEN_MODE mode, // 파일 모드는 작동하지 않지만, 향후 확장성을 위해 인자로 유지
    BOOL keepResident,
    void* refout);

__declspec(dllexport) void CloseDumpFile(DUMP_FILE_INFO* Info);

__declspec(dllexport) BOOL GetMappedModules(const DUMP_FILE_INFO* dumpInfo,
    void* outEntryAddress,
    ULONG32* outModuleCount);

_declspec(dllexport) BOOL GetMappedException(const DUMP_FILE_INFO* dumpInfo,
    void* outExceptionStream);

_declspec(dllexport) BOOL GetMappedThreadStack(const DUMP_FILE_INFO* dumpInfo,
    int ThreadID, void* outStack, void* outStackSize);


#ifdef __cplusplus
}
#endif
