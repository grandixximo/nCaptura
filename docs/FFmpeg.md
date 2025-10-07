# FFmpeg

> It is recommended to always download the latest version of FFmpeg using FFmpeg Downloader. Older versions of FFmpeg can cause unexpected behaviour.

[FFmpeg](http://ffmpeg.org/) is an open-source cross-platform solution to record, convert and stream audio and video.
It adds support for more output formats like **H.264** for Video and **Mp3**, **AAC** etc. when capturing **Only Audio**.

## Hardware Encoding Support

Captura supports hardware-accelerated encoding through FFmpeg, which significantly improves performance and reduces CPU usage:

- **AMD GPUs**: AMD Advanced Media Framework (AMF) - Supports H.264, HEVC (H.265), and AV1
  - Inspired by OBS Studio's AMD hardware encoding implementation
  - Requires AMD Radeon HD 7000 series or newer, or APU with GCN architecture
  - Ensure your AMD drivers are up to date for best performance

- **NVIDIA GPUs**: NVENC - Supports H.264 and HEVC (H.265)
  - Check NVIDIA's website to verify your GPU supports NVENC

- **Intel CPUs**: Intel QuickSync (QSV) - Supports HEVC (H.265)
  - Requires Skylake generation or later processor with integrated graphics

Hardware encoding offloads the video encoding work to your GPU, allowing for smoother captures and better performance, especially at higher resolutions and frame rates.

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