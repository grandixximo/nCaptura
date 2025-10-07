using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Captura.Video;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using Device = SharpDX.Direct3D11.Device;

namespace Captura.Windows.MediaFoundation
{
    [Flags]
    enum MftEnumFlag
    {
        SyncMFT = 0x00000001,
        AsyncMFT = 0x00000002,
        Hardware = 0x00000004,
        FieldOfUse = 0x00000008,
        LocalMFT = 0x00000010,
        TranscodeOnly = 0x00000020,
        SortAndFilter = 0x00000040,
        SortAndFilterApprovedOnly = 0x000000C0,
        SystemHardware = 0x00000100,
        All = 0x0000003F
    }

    // P/Invoke for MFTEnumEx (not available in SharpDX)
    static class MfNative
    {
        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFTEnumEx(
            Guid guidCategory,
            int flags,
            IntPtr pInputType,
            IntPtr pOutputType,
            out IntPtr pppMFTActivate,
            out int pnumMFTActivate);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class MfWriterProvider : IVideoWriterProvider
    {
        readonly Device _device;
        readonly bool _isCompatible;
        readonly string _warningMessage;
        readonly bool _hasHardwareEncoder;
        readonly MfSettings _settings;

        public MfWriterProvider(WindowsSettings WindowsSettings)
        {
            _settings = WindowsSettings.MediaFoundation;
            
            try
            {
                // Check if hardware H.264 encoder is available FIRST
                var encoderInfo = DetectHardwareEncoder();
                _hasHardwareEncoder = encoderInfo.IsAvailable;
                _isCompatible = encoderInfo.IsAvailable;
                _warningMessage = encoderInfo.Message;

                // Only try to create device if hardware encoder exists
                if (_isCompatible)
                {
                    _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                }
            }
            catch (Exception ex)
            {
                _isCompatible = false;
                _hasHardwareEncoder = false;
                _warningMessage = $"Failed to initialize Media Foundation: {ex.Message}";
                _device = null;
            }
        }

        static (bool IsAvailable, string Message) DetectHardwareEncoder()
        {
            try
            {
                // Get GPU info for better error messages
                var gpuName = GetGPUName();

                // Query Media Foundation for hardware H.264 encoder
                var hasHardwareEncoder = CheckForH264HardwareEncoder();

                if (hasHardwareEncoder)
                {
                    return (true, null); // Hardware encoder available, no warning
                }
                else
                {
                    // No hardware encoder found
                    var message = string.IsNullOrEmpty(gpuName) 
                        ? "No hardware H.264 encoder found. Use FFmpeg for better compatibility."
                        : $"No hardware H.264 encoder found for {gpuName}. Use FFmpeg instead.";
                    
                    return (false, message);
                }
            }
            catch (Exception ex)
            {
                // If detection fails, disable MF to be safe
                return (false, $"Failed to detect hardware encoder: {ex.Message}");
            }
        }

        static string GetGPUName()
        {
            try
            {
                using var factory = new Factory1();
                var adapter = factory.Adapters1.FirstOrDefault();
                return adapter?.Description.Description ?? "";
            }
            catch
            {
                return "";
            }
        }

        static bool CheckForH264HardwareEncoder()
        {
            // Simplified - just use the main CheckForHardwareEncoder method
            return CheckForHardwareEncoder(VideoFormatGuids.H264);
        }

        public string Name => "MF";

        public IEnumerator<IVideoWriterItem> GetEnumerator()
        {
            // Only provide MF options if compatible
            if (_isCompatible && _device != null)
            {
                // Detect all available hardware encoders
                var availableEncoders = DetectAllHardwareEncoders();
                
                // Filter by selected encoder if specified
                var selectedEncoder = _settings?.SelectedEncoder;
                if (!string.IsNullOrEmpty(selectedEncoder))
                {
                    var matchingEncoder = availableEncoders.FirstOrDefault(e => e.CodecName == selectedEncoder);
                    if (matchingEncoder != default)
                    {
                        yield return new MfItem(_device, matchingEncoder.CodecName, matchingEncoder.FormatGuid, matchingEncoder.Extension, _warningMessage);
                        yield break;
                    }
                }

                // If no selection or selection not found, offer all encoders
                foreach (var encoder in availableEncoders)
                {
                    yield return new MfItem(_device, encoder.CodecName, encoder.FormatGuid, encoder.Extension, _warningMessage);
                }
            }
        }

        static List<(string CodecName, Guid FormatGuid, string Extension)> DetectAllHardwareEncoders()
        {
            var encoders = new List<(string, Guid, string)>();

            // Check for H.264 hardware encoder
            if (CheckForHardwareEncoder(VideoFormatGuids.H264))
                encoders.Add(("H.264", VideoFormatGuids.H264, ".mp4"));

            // Check for H.265 (HEVC) hardware encoder
            if (CheckForHardwareEncoder(VideoFormatGuids.Hevc))
                encoders.Add(("H.265 (HEVC)", VideoFormatGuids.Hevc, ".mp4"));

            // VP9 GUID (not in SharpDX VideoFormatGuids)
            var vp9Guid = new Guid("A3DF5476-2858-4B1D-B9DC-0FC9E7F4F3F5");
            if (CheckForHardwareEncoder(vp9Guid))
                encoders.Add(("VP9", vp9Guid, ".webm"));

            // Fallback: If no encoders found but we got here, at least offer H.264
            if (encoders.Count == 0)
                encoders.Add(("H.264", VideoFormatGuids.H264, ".mp4"));

            return encoders;
        }

        static bool CheckForHardwareEncoder(Guid codecGuid)
        {
            IntPtr pActivate = IntPtr.Zero;
            
            try
            {
                var flags = (int)(MftEnumFlag.Hardware | MftEnumFlag.SortAndFilter);
                
                // Create output type for the codec we're looking for
                var outputType = new MediaType();
                outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                outputType.Set(MediaTypeAttributeKeys.Subtype, codecGuid);
                
                // Get native pointer for MFT_REGISTER_TYPE_INFO
                var pOutputType = outputType.NativePointer;

                // Call native MFTEnumEx
                var result = MfNative.MFTEnumEx(
                    TransformCategoryGuids.VideoEncoder,
                    flags,
                    IntPtr.Zero,  // No input type restriction
                    pOutputType,  // Output must be our codec
                    out pActivate,
                    out var count);

                outputType.Dispose();

                // Release the MFT activate array
                if (pActivate != IntPtr.Zero)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var activatePtr = Marshal.ReadIntPtr(pActivate, i * IntPtr.Size);
                        if (activatePtr != IntPtr.Zero)
                            Marshal.Release(activatePtr);
                    }
                    Marshal.FreeCoTaskMem(pActivate);
                }

                return result == 0 && count > 0;
            }
            catch
            {
                if (pActivate != IntPtr.Zero)
                {
                    try { Marshal.FreeCoTaskMem(pActivate); } catch { }
                }
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Name;

        public IVideoWriterItem ParseCli(string Cli)
        {
            return Cli == "mf" && _isCompatible ? this.First() : null;
        }

        public string Description
        {
            get
            {
                if (!_isCompatible)
                    return $"Media Foundation (Disabled: {_warningMessage})";
                
                if (!string.IsNullOrEmpty(_warningMessage))
                    return $"Hardware encoders - {_warningMessage}";
                
                return "Hardware-accelerated video encoding using Media Foundation";
            }
        }

        public IEnumerable<string> GetAvailableEncoderNames()
        {
            // Don't require _device for listing encoders - just detect what's available
            var encoders = DetectAllHardwareEncoders();
            
            if (encoders.Count == 0)
                return Enumerable.Empty<string>();

            return encoders.Select(encoder => encoder.CodecName).ToList();
        }
    }
}