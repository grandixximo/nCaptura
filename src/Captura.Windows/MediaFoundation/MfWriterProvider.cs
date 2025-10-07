using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Captura.Video;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Captura.Windows.MediaFoundation
{
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
                // Try to create device
                _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
                
                // Check GPU compatibility
                var gpuInfo = DetectGPU();
                _isCompatible = gpuInfo.IsCompatible;
                _warningMessage = gpuInfo.Warning;
            }
            catch (Exception ex)
            {
                _isCompatible = false;
                _warningMessage = $"Failed to initialize Media Foundation: {ex.Message}";
            }
        }

        static (bool IsCompatible, string Warning) DetectGPU()
        {
            try
            {
                using var factory = new Factory1();
                var adapter = factory.Adapters1.FirstOrDefault();
                
                if (adapter == null)
                    return (false, "No GPU adapter found");

                var desc = adapter.Description;
                var vendorId = desc.VendorId;
                var deviceName = desc.Description;

                // Check for AMD integrated graphics (known to have issues with MF)
                // VendorId: 0x1002 = AMD, 0x8086 = Intel, 0x10DE = NVIDIA
                if (vendorId == 0x1002 && (deviceName.Contains("Radeon") && !deviceName.Contains("RX")))
                {
                    return (false, $"⚠️ AMD integrated graphics detected ({deviceName}). Media Foundation may not work. Use FFmpeg instead.");
                }

                // Warn for Intel integrated graphics (may have issues)
                if (vendorId == 0x8086)
                {
                    return (true, $"⚠️ Intel graphics detected ({deviceName}). If recording fails, use FFmpeg x264 instead.");
                }

                // NVIDIA and AMD discrete GPUs should work fine
                return (true, null);
            }
            catch
            {
                return (true, null); // If detection fails, allow user to try
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