#include "FileSystem.h"


// 맵핑 초기화 함수 - 모든 필드를 초기 상태로 설정
static void FileSystem_ResetMappedFile(MAPPED_FILE_BUFFER* buffer)
{
    if (buffer == NULL)
    {
        return;
    }

    buffer->fileHandle = NULL;
    buffer->mappingHandle = NULL;
    buffer->data = NULL;
    buffer->size = 0;
    buffer->mode = FILE_OPEN_MODE_DEFAULT;
}

// 맵핑 해제 함수 - 열린 핸들과 매핑된 뷰를 안전하게 닫고 초기화
void FileSystem_CloseMappedFile(MAPPED_FILE_BUFFER* buffer)
{
    if (buffer == NULL)
    {
        return;
    }

    if (buffer->data != NULL)
    {
        (void)UnmapViewOfFile(buffer->data);
    }

    if (buffer->mappingHandle != NULL)
    {
        (void)CloseHandle(buffer->mappingHandle);
    }

    if (buffer->fileHandle != NULL)
    {
        (void)CloseHandle(buffer->fileHandle);
    }

    FileSystem_ResetMappedFile(buffer);
}

// 맵핑된 메모리를 페이지 단위로 접근하여 워밍업하는 함수
static void FileSystem_WarmupMappedMemory(const unsigned char* data, size_t size)
{
    if (data == NULL || size == 0)
    {
        return;
    }

    SYSTEM_INFO systemInfo;
    GetSystemInfo(&systemInfo);

    size_t pageSize = (size_t)systemInfo.dwPageSize;
    if (pageSize == 0)
    {
        pageSize = 4096;
    }

    // 페이지 단위로 한 번씩 접근해 메모리 상주를 유도
    volatile unsigned char sink = 0;
    size_t i = 0;
    while (i < size)
    {
        sink ^= data[i];
        i += pageSize;
    }

    sink ^= data[size - 1];
    (void)sink;

    // 가능하면 워킹셋에 잠금 시도(실패해도 동작에는 영향 없음)
    (void)VirtualLock((LPVOID)data, size);
}

// 파일을 맵핑하여 읽기 전용으로 여는 함수
BOOL FileSystem_OpenMappedFile(
    const wchar_t* filePath,
    FILE_OPEN_MODE mode,
    BOOL keepResident,
    MAPPED_FILE_BUFFER* outBuffer)
{
    LARGE_INTEGER fileSize;
    DWORD high = 0;
    DWORD low = 0;

    if (filePath == NULL || outBuffer == NULL)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    FileSystem_ResetMappedFile(outBuffer);

    outBuffer->fileHandle = CreateFileW(
        filePath,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_RANDOM_ACCESS,
        NULL);

    if (outBuffer->fileHandle == NULL)
    {
        return FALSE;
    }

    if (!GetFileSizeEx(outBuffer->fileHandle, &fileSize))
    {
        FileSystem_CloseMappedFile(outBuffer);
        return FALSE;
    }

    if (fileSize.QuadPart < 0)
    {
        SetLastError(ERROR_FILE_INVALID);
        FileSystem_CloseMappedFile(outBuffer);
        return FALSE;
    }

    outBuffer->size = (size_t)fileSize.QuadPart;
    outBuffer->mode = mode;

    // 0바이트 파일은 핸들만 유지하고 성공 처리
    if (outBuffer->size == 0)
    {
        return TRUE;
    }

    high = (DWORD)(((ULONG64)fileSize.QuadPart >> 32) & 0xFFFFFFFFu);
    low = (DWORD)((ULONG64)fileSize.QuadPart & 0xFFFFFFFFu);

    outBuffer->mappingHandle = CreateFileMappingW(
        outBuffer->fileHandle,
        NULL,
        PAGE_READONLY,
        high,
        low,
        NULL);

    if (outBuffer->mappingHandle == NULL)
    {
        FileSystem_CloseMappedFile(outBuffer);
        return FALSE;
    }

    outBuffer->data = (LPVOID)MapViewOfFile(
        outBuffer->mappingHandle,
        FILE_MAP_READ,
        0,
        0,
        0);

    if (outBuffer->data == NULL)
    {
        FileSystem_CloseMappedFile(outBuffer);
        return FALSE;
    }

    if (keepResident)
    {
        FileSystem_WarmupMappedMemory(outBuffer->data, outBuffer->size);
    }

    return TRUE;
}

size_t FileSystem_GetMappedSize(const MAPPED_FILE_BUFFER* buffer)
{
    if (buffer == NULL)
    {
        return 0;
    }

    return buffer->size;
}