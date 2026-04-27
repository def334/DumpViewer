#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <DbgHelp.h>
#pragma comment(lib, "Dbghelp.lib")

#ifndef ARRAYSIZE
#define ARRAYSIZE(a) (sizeof(a) / sizeof((a)[0]))
#endif

#include "module.h"
#include "../Files/FileSystem.h"
#include "../Exection/Exception.h"
#include "../Thread/Thread.h"
#include "Ioctl.h"

enum dump_mode
{
    DUMP_MODE_UNKNOWN = 0,
    DUMP_MODE_MINI = 1,
    DUMP_MODE_FULL = 2,
    DUMP_MODE_USERMODE_CRASHDUMP = 3,

	DUMP_MODE_KERNEL_BASE = 10, // Base for kernel dump modes
    DUMP_MODE_KERNEL_FULL = 11,
    DUMP_MODE_KERNEL_SUMMARY = 12,
    DUMP_MODE_KERNEL_HEADER = 13,
    DUMP_MODE_KERNEL_TRIAGE = 14,
};

typedef struct _DUMP_SIGNATURE
{
    ULONG signature;
    ULONG validData;
} DUMP_SIGNATURE, *PDUMP_SIGNATURE;

// -------------------------------------------------------
// 모드별 인라인 리다이렉션
// -------------------------------------------------------

static __inline BOOL Redirect_GetMappedModules(const DUMP_FILE_INFO* dumpInfo, void* outEntryAddress, ULONG32* outModuleCount)
{
    const unsigned char* dumpBase = (const unsigned char*)dumpInfo->mappedFile.data;

    if (dumpInfo->isKernelMode)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return FALSE;
    }

    return UserModeModuleExport(dumpBase, (ULONG64*)outEntryAddress, outModuleCount);
}

static __inline BOOL Redirect_GetMappedException(const DUMP_FILE_INFO* dumpInfo, void* outExceptionStream)
{
    const unsigned char* dumpBase = (const unsigned char*)dumpInfo->mappedFile.data;

    if (dumpInfo->isKernelMode)
    {
		return kernelModeExceptionExport(dumpInfo, outExceptionStream);
    }

    return UserModeExceptionExport(dumpBase, outExceptionStream);
}

static __inline BOOL Redirect_GetMappedThreads(const DUMP_FILE_INFO* dumpInfo, void* outEntryAddress, ULONG32* outThreadCount)
{
    const unsigned char* dumpBase = (const unsigned char*)dumpInfo->mappedFile.data;

    if (dumpInfo->isKernelMode)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return FALSE;
    }

    return UserModeThreadList(dumpBase, outEntryAddress, outThreadCount);
}

static __inline BOOL Redirect_GetMappedThreadStack(const DUMP_FILE_INFO* dumpInfo, int ThreadID, void* outStack, void* outStackSize)
{
    const unsigned char* dumpBase = (const unsigned char*)dumpInfo->mappedFile.data;

    if (dumpInfo->isKernelMode)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return FALSE;
    }

    return GetThreadStack(dumpBase, ThreadID, outStack, outStackSize);
}

// -------------------------------------------------------
// 공통 유효성 검사
// -------------------------------------------------------

static __inline BOOL Ioctl_ValidateDumpInfo(const DUMP_FILE_INFO* dumpInfo)
{
    if (dumpInfo == NULL || dumpInfo->mappedFile.data == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }
    return TRUE;
}

// -------------------------------------------------------
// 내부 함수
// -------------------------------------------------------

static UINT GetHeaderMode(void* mapping)
{
	ULONG signature = ((DUMP_SIGNATURE*)mapping)->signature;

    if(signature == MINIDUMP_SIGNATURE)
        return DUMP_MODE_MINI;

    if (signature == DUMP_SIGNATURE64)
    {
        DUMP_HEADER64* header = (DUMP_HEADER64*)mapping;
		return header->DumpType + DUMP_MODE_KERNEL_BASE; // Kernel dump types are offset by 10 from the base dump mode
    }

	return DUMP_MODE_UNKNOWN;
}

static BOOL GetDumpHeader(const void* Mapping, DUMP_FILE_INFO* OutBuffer)
{
    if (Mapping == NULL || OutBuffer == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

	OutBuffer->HEADER = (void*)Mapping;
	UINT type = GetHeaderMode((void*)Mapping);
    switch (type)
    {

    case DUMP_MODE_MINI:
    {
        void* exceptionStream = NULL;
        OutBuffer->dumpMode = ((MINIDUMP_HEADER*)Mapping)->Flags & MiniDumpWithFullMemory ? DUMP_MODE_FULL : DUMP_MODE_MINI;

        return TRUE;
    }
    case DUMP_MODE_KERNEL_FULL:
    case DUMP_MODE_KERNEL_SUMMARY:
    case DUMP_MODE_KERNEL_HEADER:
    case DUMP_MODE_KERNEL_TRIAGE:
    {
		OutBuffer->isKernelMode = TRUE;
        OutBuffer->dumpMode = type;
        return TRUE;
    }
    default:
        break;

    }

    SetLastError(ERROR_BAD_LENGTH);
    return FALSE;
}

// -------------------------------------------------------
// DLL 외부 공개 함수
// -------------------------------------------------------

__declspec(dllexport) void CloseDumpFile(DUMP_FILE_INFO* Info)
{
    FileSystem_CloseMappedFile(&Info->mappedFile);
}

__declspec(dllexport) BOOL OpenDumpFile(
    const wchar_t* dumpFilePath,
    FILE_OPEN_MODE mode,
    BOOL keepResident,
    void* refout)
{
    WIN32_FILE_ATTRIBUTE_DATA attr;
    DUMP_FILE_INFO tempInfo;
    const unsigned char* data;

    if (dumpFilePath == NULL || refout == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    if (mode != FILE_OPEN_MODE_BYTE)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return FALSE;
    }

    if (!GetFileAttributesExW(dumpFilePath, GetFileExInfoStandard, &attr))
        return FALSE;

    ZeroMemory(&tempInfo, sizeof(tempInfo));

	tempInfo.path = dumpFilePath;
    tempInfo.fileAttributes = attr.dwFileAttributes;
    tempInfo.creationTime = attr.ftCreationTime;
    tempInfo.lastWriteTime = attr.ftLastWriteTime;
    tempInfo.fileSize = (((ULONGLONG)attr.nFileSizeHigh) << 32) | (ULONGLONG)attr.nFileSizeLow;

    if (!FileSystem_OpenMappedFile(dumpFilePath, mode, keepResident, &tempInfo.mappedFile))
        return FALSE;

    data = (const unsigned char*)tempInfo.mappedFile.data;
    if (!GetDumpHeader(data, &tempInfo))
    {
        FileSystem_CloseMappedFile(&tempInfo.mappedFile);
		return FALSE;
    }

    memcpy(refout, &tempInfo, sizeof(DUMP_FILE_INFO));
    return TRUE;
}

__declspec(dllexport) BOOL GetMappedModules(const DUMP_FILE_INFO* dumpInfo, void* outEntryAddress, ULONG32* outModuleCount)
{
    if (!Ioctl_ValidateDumpInfo(dumpInfo)) return FALSE;
    return Redirect_GetMappedModules(dumpInfo, outEntryAddress, outModuleCount);
}

__declspec(dllexport) BOOL GetMappedException(const DUMP_FILE_INFO* dumpInfo, void* outExceptionStream)
{
    if (!Ioctl_ValidateDumpInfo(dumpInfo)) return FALSE;
    return Redirect_GetMappedException(dumpInfo, outExceptionStream);
}

__declspec(dllexport) BOOL GetMappedThreads(const DUMP_FILE_INFO* dumpInfo, void* outEntryAddress, ULONG32* outThreadCount)
{
    if (!Ioctl_ValidateDumpInfo(dumpInfo)) return FALSE;
    return Redirect_GetMappedThreads(dumpInfo, outEntryAddress, outThreadCount);
}

__declspec(dllexport) BOOL GetMappedThreadStack(const DUMP_FILE_INFO* dumpInfo, int ThreadID, void* outStack, void* outStackSize)
{
    if (!Ioctl_ValidateDumpInfo(dumpInfo)) return FALSE;
    return Redirect_GetMappedThreadStack(dumpInfo, ThreadID, outStack, outStackSize);
}