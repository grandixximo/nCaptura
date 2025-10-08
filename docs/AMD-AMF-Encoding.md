# AMD AMF Hardware Encoding

AMD Advanced Media Framework (AMF) provides hardware-accelerated video encoding on AMD GPUs, significantly improving performance and reducing CPU usage during screen recording.

## Requirements

- **AMD GPU**: Radeon HD 7000 series or newer, or APU with GCN architecture
  - Your system: AMD Ryzen 7 4800U with Radeon Graphics ✅ (Supported)
- **Updated AMD Drivers**: Ensure you have the latest AMD graphics drivers
- **FFmpeg with AMF Support**: Your FFmpeg build must be compiled with AMF support

## Checking FFmpeg AMF Support

To verify your FFmpeg has AMF support, open a command prompt and run:

```bash
ffmpeg -encoders | findstr amf
```

You should see output like:
```
 V..... h264_amf             AMD AMF H.264 Encoder (codec h264)
 V..... hevc_amf             AMD AMF HEVC encoder (codec hevc)
```

If you don't see this output, your FFmpeg build doesn't have AMF support compiled in.

## Getting FFmpeg with AMF Support

### Option 1: Download Pre-built FFmpeg with AMF
Download FFmpeg from these sources which include AMF support:
- **Recommended**: https://www.gyan.dev/ffmpeg/builds/ (Full build includes AMF)
- Alternative: https://github.com/BtbN/FFmpeg-Builds/releases (Look for builds with "amf" in the name)

### Option 2: Use Captura's FFmpeg Downloader
Captura's built-in FFmpeg downloader should provide a build with AMF support.

## Troubleshooting

### Error: "Exit Code: -22" (EINVAL - Invalid Argument)

This error indicates FFmpeg rejected some encoding parameters. Try these solutions:

1. **Try the VBR variant**: 
   - Instead of "AMD AMF: Mp4 (H.264, AAC)"
   - Try "AMD AMF VBR: Mp4 (H.264, AAC)"
   - VBR mode uses simpler parameters that work on more systems

2. **Check FFmpeg has AMF support**:
   ```bash
   ffmpeg -h encoder=h264_amf
   ```
   If this shows an error, your FFmpeg doesn't have AMF support.

3. **Update AMD drivers**:
   - Download latest drivers from: https://www.amd.com/en/support
   - Older drivers may not expose AMF properly to FFmpeg

4. **Try different quality settings**:
   - If using custom FFmpeg commands, adjust the `qp` value (18-28 range)
   - Lower = better quality but larger files
   - Higher = smaller files but lower quality

### Video Not Being Saved

If the recording appears to run but no file is saved:

1. Check the FFmpeg Log (available in Captura's FFmpeg page)
2. Look for specific error messages about AMF initialization
3. Verify your GPU is not being used by other applications
4. Try closing GPU-intensive applications and retry

### Alternative: Use NVENC or Software Encoding

If AMF continues to have issues:
- **Intel users**: Try "Intel QuickSync" options
- **NVIDIA users**: Try "NVenc" options  
- **Software fallback**: Use "x264" (CPU encoding, slower but always works)

## AMF Codec Options in Captura

Captura provides **three different AMF implementations** for maximum compatibility. Try them in this order:

### 1. AMD AMF Simple (RECOMMENDED - Try This First)
- **Option**: "AMD AMF Simple: Mp4 (H.264, AAC)"
- **Settings**: Uses only the codec name, no extra parameters
- **Compatibility**: Highest - works with most FFmpeg builds
- **Quality**: Good (uses encoder defaults)
- **File Size**: Reasonable (encoder decides bitrate automatically)
- ✅ **Start here** - if this works, you're all set!

### 2. AMD AMF CQP (Constant Quality)
- **Option**: "AMD AMF: Mp4 (H.264, AAC)"
- **Settings**: Uses CQP (Constant Quantization Parameter) mode with QP=22
- **Compatibility**: Good - works with most AMF-enabled FFmpeg builds
- **Quality**: Excellent and consistent
- **File Size**: Variable (adjusts to maintain quality)
- Try this if you want better quality control

### 3. AMD AMF VBR (Variable Bitrate)
- **Option**: "AMD AMF VBR: Mp4 (H.264, AAC)"
- **Settings**: Uses VBR mode with fixed bitrate target
  - H.264: 5 Mbps target
  - HEVC: 4 Mbps target (HEVC compresses better)
- **Compatibility**: Good - different parameter set
- **Quality**: Good, depends on content complexity
- **File Size**: Predictable (5 Mbps ≈ 2.25 GB/hour at 1080p)
- Try this if CQP mode gives errors

### HEVC/H.265 Variants
All three modes are also available for HEVC (H.265):
- Better compression than H.264 (30-50% smaller files)
- Same quality at lower bitrate
- Requires slightly more modern GPU
- May have less compatibility in video players

## OBS Studio Comparison

This implementation is inspired by OBS Studio's AMD encoding. If OBS Studio works on your system:

1. OBS uses AMF through a similar FFmpeg integration
2. If OBS works but Captura doesn't, it's likely an FFmpeg build difference
3. Ensure you're using a full FFmpeg build (not minimal/lite versions)

## Performance Tips

When using AMF encoding:
- **CPU usage** should drop significantly compared to x264
- **GPU usage** will increase (this is expected and good)
- **Recording quality** at default settings should be excellent for screen recording
- **File sizes** will be reasonable (5 Mbps = ~2.25 GB per hour for 1080p)

## Getting Help

If AMF encoding still doesn't work after trying these steps:

1. Check the FFmpeg Log in Captura (FFmpeg page → FFmpeg Log)
2. Run the FFmpeg encoder check command and share the output
3. Verify your AMD driver version
4. Report the issue with your hardware details and FFmpeg version
