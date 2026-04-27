#pragma once

#include "../Dump/Internel.h"

#ifdef __cplusplus
extern "C" {
#endif

    BOOL FileSystem_OpenMappedFile(
        const wchar_t* filePath,
        FILE_OPEN_MODE mode,
        BOOL keepResident,
        MAPPED_FILE_BUFFER* outBuffer);

    void FileSystem_CloseMappedFile(MAPPED_FILE_BUFFER* buffer);
    size_t FileSystem_GetMappedSize(const MAPPED_FILE_BUFFER* buffer);

#ifdef __cplusplus
}
#endif
