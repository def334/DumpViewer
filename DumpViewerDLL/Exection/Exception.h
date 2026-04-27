#pragma once

int UserModeExceptionExport(const void* mappingAddr, void* outExceptionStream);
int kernelModeExceptionExport(const void* dumpHeader, void* outExceptionStream);