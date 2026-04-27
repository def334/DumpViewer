#pragma once
#include "Ioctl.h"

#ifdef __cplusplus
extern "C" {
#endif

    BOOL UserModeModuleExport(const void* mappingAddr, ULONG64* outListEntryAddr, ULONG32* outListCount);


#ifdef __cplusplus
}
#endif