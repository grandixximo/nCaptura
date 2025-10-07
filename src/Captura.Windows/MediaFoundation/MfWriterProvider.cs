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

        public MfWriterProvider()
        {
            try
            {
                // Check for hardware encoder for messaging, but do not disable MF if absent
                var encoderInfo = DetectHardwareEncoder();
                _hasHardwareEncoder = encoderInfo.IsAvailable;
                _warningMessage = encoderInfo.Message;

                // Create a D3D device for capture/processing regardless of hardware encoder presence
                _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                _isCompatible = true;
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
            // Only provide MF options if initialized successfully
            if (_isCompatible && _device != null)
            {
                // Detect all available hardware encoders
                var availableEncoders = DetectAllHardwareEncoders();

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
            if (CheckForHardwareEncoderDetailed(VideoFormatGuids.H264, out var h264Error))
            {
                encoders.Add(("H.264", VideoFormatGuids.H264, ".mp4"));
            }
            else
            {
                // Log the reason H.264 failed (for debugging)
                System.Diagnostics.Debug.WriteLine($"H.264 hardware encoder check failed: {h264Error}");
            }

            // Check for H.265 (HEVC) hardware encoder
            if (CheckForHardwareEncoderDetailed(VideoFormatGuids.Hevc, out var hevcError))
            {
                encoders.Add(("H.265 (HEVC)", VideoFormatGuids.Hevc, ".mp4"));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"H.265 hardware encoder check failed: {hevcError}");
            }

            // VP9 GUID (not in SharpDX VideoFormatGuids)
            var vp9Guid = new Guid("A3DF5476-2858-4B1D-B9DC-0FC9E7F4F3F5");
            if (CheckForHardwareEncoderDetailed(vp9Guid, out var vp9Error))
            {
                encoders.Add(("VP9", vp9Guid, ".webm"));
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"VP9 hardware encoder check failed: {vp9Error}");
            }

            // Fallback: If no hardware encoders found, still offer H.264 for software MFTs
            if (encoders.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No hardware encoders detected, falling back to software H.264");
                encoders.Add(("H.264", VideoFormatGuids.H264, ".mp4"));
            }

            return encoders;
        }

        static bool CheckForHardwareEncoder(Guid codecGuid) => CheckForHardwareEncoderDetailed(codecGuid, out _);

        static bool CheckForHardwareEncoderDetailed(Guid codecGuid, out string errorMessage)
        {
            errorMessage = null;
            IntPtr pActivate = IntPtr.Zero;
            Activate activate = null;
            MediaFoundation.Transform transform = null;
            MediaType inputType = null;
            MediaType outputType = null;
            
            try
            {
                var flags = (int)(MftEnumFlag.Hardware | MftEnumFlag.SortAndFilter);
                
                // Create output type for the codec we're looking for
                outputType = new MediaType();
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

                if (result != 0)
                {
                    errorMessage = $"MFTEnumEx failed with HRESULT 0x{result:X8}";
                    return false;
                }

                if (count == 0)
                {
                    errorMessage = "No hardware encoders enumerated for this codec";
                    return false;
                }

                // Try to actually instantiate the first encoder to verify it works
                var firstActivatePtr = Marshal.ReadIntPtr(pActivate, 0);
                if (firstActivatePtr == IntPtr.Zero)
                {
                    errorMessage = "Encoder activate pointer is null";
                    return false;
                }

                activate = new Activate(firstActivatePtr);
                
                // Actually activate the transform (this tests if it can be created)
                try
                {
                    transform = activate.ActivateObject<MediaFoundation.Transform>();
                }
                catch (Exception ex)
                {
                    errorMessage = $"Failed to activate encoder: {ex.Message}";
                    return false;
                }
                
                if (transform == null)
                {
                    errorMessage = "Encoder transform is null after activation";
                    return false;
                }

                // Try to set basic input/output types to verify encoder is functional
                // Create a test input type (NV12 is widely supported)
                inputType = new MediaType();
                inputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                inputType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.NV12);
                inputType.Set(MediaTypeAttributeKeys.FrameSize, ((long)1920 << 32) | 1080);
                inputType.Set(MediaTypeAttributeKeys.FrameRate, ((long)30 << 32) | 1);
                inputType.Set(MediaTypeAttributeKeys.InterlaceMode, (int)VideoInterlaceMode.Progressive);

                // Try setting the input type - if this fails, encoder is not functional
                try
                {
                    transform.SetInputType(0, inputType, 0);
                }
                catch (Exception ex)
                {
                    errorMessage = $"Encoder failed to accept input format: {ex.Message}";
                    return false;
                }

                // Encoder enumerated and successfully instantiated
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Unexpected error: {ex.Message}";
                return false;
            }
            finally
            {
                // Clean up all COM objects
                inputType?.Dispose();
                outputType?.Dispose();
                transform?.Dispose();
                activate?.Dispose();
                
                // Release the MFT activate array
                if (pActivate != IntPtr.Zero)
                {
                    try
                    {
                        // Release up to 10 activate objects (reasonable max)
                        for (int i = 0; i < 10; i++)
                        {
                            var activatePtr = Marshal.ReadIntPtr(pActivate, i * IntPtr.Size);
                            if (activatePtr == IntPtr.Zero)
                                break;
                            Marshal.Release(activatePtr);
                        }
                        Marshal.FreeCoTaskMem(pActivate);
                    }
                    catch { }
                }
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

                if (_hasHardwareEncoder)
                    return "Hardware-accelerated video encoding using Media Foundation";

                // Hardware not available, but software fallback may still work
                return string.IsNullOrEmpty(_warningMessage)
                    ? "Media Foundation software encoding (hardware encoder not detected)"
                    : $"Media Foundation - {_warningMessage}";
            }
        }
    }
}