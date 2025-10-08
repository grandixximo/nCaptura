# FFmpeg

> It is recommended to always download the latest version of FFmpeg using FFmpeg Downloader. Older versions of FFmpeg can cause unexpected behaviour.

[FFmpeg](http://ffmpeg.org/) is an open-source cross-platform solution to record, convert and stream audio and video.
It adds support for more output formats like **H.264** for Video and **Mp3**, **AAC** etc. when capturing **Only Audio**.

## Screen Capture Methods

Captura offers multiple screen capture methods (Settings → Video page):

- **Windows Graphics Capture (WGC)** - Modern API (Windows 10 1903+)
  - ✅ Recommended for all modern systems
  - Hardware-accelerated, reliable, best for AMD hardware
  - Default on supported systems

- **Desktop Duplication** - Legacy API (Windows 8+)
  - Hardware-accelerated but can have issues on some AMD systems
  - Fallback if WGC unavailable

- **GDI** - Legacy software capture
  - ⚠️ Slow, causes frame drops on modern hardware
  - Only use if both WGC and Desktop Duplication fail

## Hardware Encoding Support

Captura supports hardware-accelerated encoding through FFmpeg:

- **AMD GPUs**: AMF - H.264, HEVC (H.265), AV1
  - Requires AMD Radeon HD 7000+ or APU with GCN architecture
  - Requires FFmpeg built with AMF support

- **NVIDIA GPUs**: NVENC - H.264, HEVC (H.265)

- **Intel CPUs**: QuickSync (QSV) - HEVC (H.265)
  - Requires Skylake generation or later with integrated graphics

### Checking Hardware Encoder Support

To verify your FFmpeg supports a specific hardware encoder, run:
```bash
ffmpeg -encoders | findstr <encoder_name>
```

Replace `<encoder_name>` with:
- `amf` for AMD
- `nvenc` for NVIDIA
- `qsv` for Intel QuickSync

If you don't see output, your FFmpeg build doesn't include that encoder. Download a full build from the sources listed below.

FFmpeg is configured on the **FFmpeg** section in the **Configure** tab.

Due to its large size (approx. 30MB), it is not included in the downloads.
If you already have FFmpeg on your system, you can just set the path to the folder containing it.
If it is installed globally (available in PATH), you don't have to do anything.
If you don't have FFmpeg or want to update, use the inbuilt **FFmpeg Downloader**.
FFmpeg needs to be downloaded only once.

In cases where the **FFmpeg Downloader** fails, please download manually from one of these sources:
- <https://www.gyan.dev/ffmpeg/builds/> (Recommended - Windows builds)
- <https://github.com/BtbN/FFmpeg-Builds/releases> (Alternative Windows builds)
- <https://ffmpeg.org/download.html> (Official FFmpeg site with links to all builds)

After downloading, extract the archive and set the FFmpeg folder (containing `ffmpeg.exe`) in `Configure | FFmpeg`.

If you don't want to use FFmpeg, you can switch to `SharpAvi`.