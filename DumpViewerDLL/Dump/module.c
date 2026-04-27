#include <Windows.h>
#include <DbgHelp.h>

#include "module.h"
#include "../Files/FileSystem.h"

// User-mode dump extract function: Extracts the base addresses of loaded modules from a user-mode minidump file.
BOOL UserModeModuleExport(
    const void* mappingAddr,
    ULONG64* outListEntryAddr,
    ULONG32* outListCount)
{
    if(mappingAddr == NULL || outListEntryAddr == NULL || outListCount == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
	}

    const unsigned char* dumpBase = NULL;
    ULONG32 dumpSize = 0;

    PMINIDUMP_DIRECTORY directory = NULL;
    PVOID stream = NULL;
    ULONG streamSize = 0;

	MINIDUMP_MODULE_LIST* moduleList = NULL;

    dumpBase = mappingAddr;
    dumpSize = FileSystem_GetMappedSize(&mappingAddr);

    if (dumpBase == NULL || dumpSize < sizeof(MINIDUMP_HEADER))
    {
        SetLastError(ERROR_BAD_FORMAT);
        return FALSE;
    }

    if (!MiniDumpReadDumpStream(
        (PVOID)dumpBase,
        ModuleListStream,
        &directory,
        &stream,
        &streamSize))
    {
        SetLastError(ERROR_NOT_FOUND);
        return FALSE;
    }

    if (stream == NULL || streamSize < sizeof(MINIDUMP_MODULE_LIST))
    {
        SetLastError(ERROR_BAD_FORMAT);
        return FALSE;
    }

    moduleList = (MINIDUMP_MODULE_LIST*)stream;

	*outListEntryAddr = (ULONG64)&moduleList->Modules[0];
	*outListCount = moduleList->NumberOfModules;

    return TRUE;
}