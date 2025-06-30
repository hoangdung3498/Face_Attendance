// CustomOpsStub.m
// Stub implementation for RegisterCustomOps to resolve macCatalyst linker errors
// Based on ONNX Runtime custom operations requirements for arm64/x64 macCatalyst

#import <Foundation/Foundation.h>

// Forward declaration to match ONNX Runtime expectation
extern void RegisterCustomOps(void*, void*);

// Stub implementation - empty function to satisfy linker
void RegisterCustomOps(void* options, void* provider) {
    // Empty implementation - ONNX Runtime 1.22.0 expects this symbol
    // but doesn't actually use custom ops in this configuration
    NSLog(@"RegisterCustomOps stub called - no custom operations registered");
} 