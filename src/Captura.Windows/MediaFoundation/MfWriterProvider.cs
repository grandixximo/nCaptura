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

    // ReSharper disable once ClassNeverInstantiated.Global
    public class MfWriterProvider : IVideoWriterProvider
    {
        readonly Device _device;
        readonly bool _isCompatible;
        readonly string _warningMessage;

        public MfWriterProvider()
        {
            try
            {
                // Check if hardware H.264 encoder is available FIRST
                var encoderInfo = DetectHardwareEncoder();
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
            try
            {
                // Query for hardware video encoders
                var flags = (int)(MftEnumFlag.Hardware | MftEnumFlag.SortAndFilter);
                
                // Input type: Uncompressed video (NV12 or RGB32)
                var inputType = new MediaType();
                inputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                
                // Output type: H.264 compressed video
                var outputType = new MediaType();
                outputType.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                outputType.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.H264);

                // Enumerate hardware encoders that can produce H.264
                var result = MediaFactory.TEnumEx(
                    TransformCategoryGuids.VideoEncoder,
                    flags,
                    inputType,
                    outputType,
                    out var transforms,
                    out var count);

                // Clean up
                inputType.Dispose();
                outputType.Dispose();

                if (transforms != null)
                {
                    // Release COM objects
                    for (int i = 0; i < count; i++)
                    {
                        if (transforms[i] != null)
                            Marshal.ReleaseComObject(transforms[i]);
                    }
                }

                // If we found any hardware H.264 encoders, return true
                return result == 0 && count > 0;
            }
            catch
            {
                // If enumeration fails, assume no hardware encoder
                return false;
            }
        }

        public string Name => "MF";

        public IEnumerator<IVideoWriterItem> GetEnumerator()
        {
            // Only provide MF option if compatible
            if (_isCompatible && _device != null)
            {
                yield return new MfItem(_device, _warningMessage);
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
                    return $"H.264 Hardware encoder - {_warningMessage}";
                
                return "Encode to Mp4: H.264 with AAC audio using Media Foundation Hardware encoder";
            }
        }
    }
}