#pragma once

struct DXGIContext;

extern "C"
{
    __declspec(dllexport)
        DXGIContext* __cdecl InitContext();

    __declspec(dllexport)
        unsigned char* __cdecl GrabFramePointer(
            DXGIContext* ctx,
            int left,
            int top,
            int right,
            int bottom,
            int* outRowPitch);

    __declspec(dllexport)
        void __cdecl UnlockFramePointer(
            DXGIContext* ctx);

    __declspec(dllexport)
        void __cdecl CloseContext(
            DXGIContext* ctx);
}