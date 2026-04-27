#include <windows.h>
#include <DbgHelp.h>
#pragma comment(lib, "Dbghelp.lib")

#include "../Asm/asm.h"
#include "../Dump/Internel.h"

// intnel structure 

struct _UNICODE_STRING
{
    USHORT Length;                                                          //0x0
    USHORT MaximumLength;                                                   //0x2
    WCHAR* Buffer;                                                          //0x8
};

typedef struct _KLDR_DATA_TABLE_ENTRY
{
    struct _LIST_ENTRY InLoadOrderLinks;                                    //0x0
    VOID* ExceptionTable;                                                   //0x10
    ULONG ExceptionTableSize;                                               //0x18
    VOID* GpValue;                                                          //0x20
    struct _NON_PAGED_DEBUG_INFO* NonPagedDebugInfo;                        //0x28
    VOID* DllBase;                                                          //0x30
    VOID* EntryPoint;                                                       //0x38
    ULONG SizeOfImage;                                                      //0x40
    struct _UNICODE_STRING FullDllName;                                     //0x48
    struct _UNICODE_STRING BaseDllName;                                     //0x58
    ULONG Flags;                                                            //0x68
    USHORT LoadCount;                                                       //0x6c
    union
    {
        USHORT SignatureLevel : 4;                                            //0x6e
        USHORT SignatureType : 3;                                             //0x6e
        USHORT Frozen : 2;                                                    //0x6e
        USHORT HotPatch : 1;                                                  //0x6e
        USHORT Unused : 6;                                                    //0x6e
        USHORT EntireField;                                                 //0x6e
    } u1;                                                                   //0x6e
    VOID* SectionPointer;                                                   //0x70
    ULONG CheckSum;                                                         //0x78
    ULONG CoverageSectionSize;                                              //0x7c
    VOID* CoverageSection;                                                  //0x80
    VOID* LoadedImports;                                                    //0x88
    union
    {
        VOID* Spare;                                                        //0x90
        struct _KLDR_DATA_TABLE_ENTRY* NtDataTableEntry;                    //0x90
    };
    ULONG SizeOfImageNotRounded;                                            //0x98
    ULONG TimeDateStamp;                                                    //0x9c
} KLDR_DATA_TABLE_ENTRY, *PKLDR_DATA_TABLE_ENTRY;


// end

int UserModeExceptionExport(const void* mappingAddr, void* outExceptionStream)
{
    PMINIDUMP_DIRECTORY directory = NULL;
    PVOID stream = NULL;
    ULONG streamSize = 0;

    if (!MiniDumpReadDumpStream(
        (PVOID)mappingAddr,
        ExceptionStream,
        &directory,
        &stream,
        &streamSize))
    {
        if(GetRcx() == ERROR_NOT_FOUND)
            return TRUE;

        return FALSE;
    }

    if (stream == NULL || streamSize < sizeof(MINIDUMP_EXCEPTION_STREAM))
    {
        SetLastError(ERROR_BAD_FORMAT);
        return FALSE;
    }

	*(ULONG64*)outExceptionStream = (ULONG64)stream;
    return TRUE;
}

int kernelModeExceptionExport(const void* mappingAddr, void* outExceptionStream)
{
    if(outExceptionStream == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
	}

	DUMP_HEADER64* Header = (DUMP_HEADER64*)mappingAddr;
    SetLastError(ERROR_NOT_SUPPORTED);
    return FALSE;
}