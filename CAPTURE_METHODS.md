# Screen Capture Methods

Captura now supports three capture methods (Settings → Video page):

## 1. Windows Graphics Capture (WGC) - **Recommended**
- Modern API (Windows 10 1903+ required)
- Hardware-accelerated via GPU
- Best reliability, especially for AMD hardware
- Same technology OBS Studio uses
- **Default setting**

## 2. Desktop Duplication - Legacy
- Older API (Windows 8+ required)
- Hardware-accelerated via GPU
- May have resource issues on AMD hardware
- Can fail on second recording (known Windows API limitation)

## 3. GDI - Slowest
- Software-based CPU capture
- Works on all Windows versions
- **WARNING**: Very slow, causes frame drops
- Only use if both WGC and Desktop Duplication fail

## How to Select

**Settings → Video page → Screen Capture Method dropdown**
- Select one option
- Restart application for changes to take effect

## Capture Method Priority

Each method either works or throws a clear error (no automatic fallback):

- **If GDI selected**: Always use GDI
- **If WGC selected**: Use WGC, fail if unavailable
- **If Desktop Duplication selected**: Use Desktop Duplication, fail if unavailable

This allows clear testing and debugging of specific capture methods.

## Recommendation for AMD Hardware

Use **Windows Graphics Capture (WGC)** - it has the best compatibility with AMD GPUs.
