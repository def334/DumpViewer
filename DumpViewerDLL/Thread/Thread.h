#pragma once

int UserModeThreadList(const void* mappingAddr, void* outThreadList, void* outThreadCount);
int GetThreadStack(const void* mappingAddr, int ThreadID, void* outStack, void* outStackSize);