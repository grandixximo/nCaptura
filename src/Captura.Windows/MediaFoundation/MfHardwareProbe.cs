using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using MediaFoundation.Transform;
using Device = SharpDX.Direct3D11.Device;

namespace Captura.Windows.MediaFoundation
{
    public static class MfHardwareProbe
    {
        public class MfSupportMatrix
        {
            public string GpuName { get; set; }
            public List<MfCodecInfo> Codecs { get; set; } = new List<MfCodecInfo>();
        }

        public class MfCodecInfo
        {
            public string Codec { get; set; }
            public MfEncoderInfo Encoder { get; set; } = new MfEncoderInfo();
            public MfDecoderInfo Decoder { get; set; } = new MfDecoderInfo();
        }

        public class MfEncoderInfo
        {
            public bool HardwarePresent { get; set; }
            public bool AcceptsNV12 { get; set; }
            public bool AcceptsP010 { get; set; }
        }

        public class MfDecoderInfo
        {
            public bool HardwarePresent { get; set; }
            public bool OutputsNV12 { get; set; }
            public bool OutputsP010 { get; set; }
        }

        static readonly Guid VideoFormatP010 = new Guid("30313050-0000-0010-8000-00AA00389B71");
        static readonly Guid Vp9EncodedGuid = new Guid("A3DF5476-2858-4B1D-B9DC-0FC9E7F4F3F5");

        public static MfSupportMatrix Probe()
        {
            // Ensure MF is ready
            MfManager.Startup();

            var matrix = new MfSupportMatrix
            {
                GpuName = GetGpuName()
            };

            using var device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            using var dxgiManager = new DXGIDeviceManager();
            dxgiManager.ResetDevice(device);

            var codecs = new List<(string Name, Guid EncodedGuid)>
            {
                ("H264", VideoFormatGuids.H264),
                ("HEVC", VideoFormatGuids.Hevc),
                ("VP9", Vp9EncodedGuid)
            };

            foreach (var (name, encodedGuid) in codecs)
            {
                var info = new MfCodecInfo { Codec = name };

                // Encoders
                var encoderActivates = MediaFactory.FindTransform(
                    TransformCategoryGuids.VideoEncoder,
                    TransformEnumFlag.Hardware);

                info.Encoder.HardwarePresent = false;
                info.Encoder.AcceptsNV12 = false;
                info.Encoder.AcceptsP010 = false;

                foreach (var activate in encoderActivates)
                {
                    try
                    {
                        using var transform = activate.ActivateObject<Transform>();
                        transform.ProcessMessage(TMessageType.SetD3DManager, dxgiManager.NativePointer);

                        if (!info.Encoder.HardwarePresent && TryConfigureEncoder(transform, encodedGuid, VideoFormatGuids.NV12))
                            info.Encoder.HardwarePresent = true;

                        if (TryConfigureEncoder(transform, encodedGuid, VideoFormatGuids.NV12))
                            info.Encoder.AcceptsNV12 = true;

                        if (TryConfigureEncoder(transform, encodedGuid, VideoFormatP010))
                            info.Encoder.AcceptsP010 = true;
                    }
                    catch
                    {
                        // ignore
                    }
                    finally
                    {
                        activate.Dispose();
                    }
                }

                // Decoders
                var decoderActivates = MediaFactory.FindTransform(
                    TransformCategoryGuids.VideoDecoder,
                    TransformEnumFlag.Hardware);

                info.Decoder.HardwarePresent = false;
                info.Decoder.OutputsNV12 = false;
                info.Decoder.OutputsP010 = false;

                foreach (var activate in decoderActivates)
                {
                    try
                    {
                        using var transform = activate.ActivateObject<Transform>();
                        transform.ProcessMessage(TMessageType.SetD3DManager, dxgiManager.NativePointer);

                        if (!info.Decoder.HardwarePresent && TryConfigureDecoder(transform, encodedGuid, VideoFormatGuids.NV12))
                            info.Decoder.HardwarePresent = true;

                        if (TryConfigureDecoder(transform, encodedGuid, VideoFormatGuids.NV12))
                            info.Decoder.OutputsNV12 = true;

                        if (TryConfigureDecoder(transform, encodedGuid, VideoFormatP010))
                            info.Decoder.OutputsP010 = true;
                    }
                    catch
                    {
                        // ignore
                    }
                    finally
                    {
                        activate.Dispose();
                    }
                }

                matrix.Codecs.Add(info);
            }

            return matrix;
        }

        static bool TryConfigureEncoder(Transform transform, Guid encodedSubtype, Guid inputPixelFormat)
        {
            const int width = 1920;
            const int height = 1080;
            const int fps = 30;

            using var mediaTypeIn = new MediaType();
            mediaTypeIn.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            mediaTypeIn.Set(MediaTypeAttributeKeys.Subtype, inputPixelFormat);
            mediaTypeIn.Set(MediaTypeAttributeKeys.FrameSize, PackLong(width, height));
            mediaTypeIn.Set(MediaTypeAttributeKeys.FrameRate, PackLong(fps, 1));
            mediaTypeIn.Set(MediaTypeAttributeKeys.PixelAspectRatio, PackLong(1, 1));

            using var mediaTypeOut = new MediaType();
            mediaTypeOut.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            mediaTypeOut.Set(MediaTypeAttributeKeys.Subtype, encodedSubtype);
            mediaTypeOut.Set(MediaTypeAttributeKeys.FrameSize, PackLong(width, height));
            mediaTypeOut.Set(MediaTypeAttributeKeys.FrameRate, PackLong(fps, 1));
            mediaTypeOut.Set(MediaTypeAttributeKeys.PixelAspectRatio, PackLong(1, 1));

            try
            {
                transform.SetInputType(0, mediaTypeIn, 0);
                transform.SetOutputType(0, mediaTypeOut, 0);
                return true;
            }
            catch
            {
                try
                {
                    transform.SetOutputType(0, mediaTypeOut, 0);
                    transform.SetInputType(0, mediaTypeIn, 0);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        static bool TryConfigureDecoder(Transform transform, Guid encodedSubtype, Guid outputPixelFormat)
        {
            const int width = 1920;
            const int height = 1080;
            const int fps = 30;

            using var mediaTypeIn = new MediaType();
            mediaTypeIn.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            mediaTypeIn.Set(MediaTypeAttributeKeys.Subtype, encodedSubtype);
            mediaTypeIn.Set(MediaTypeAttributeKeys.FrameSize, PackLong(width, height));
            mediaTypeIn.Set(MediaTypeAttributeKeys.FrameRate, PackLong(fps, 1));
            mediaTypeIn.Set(MediaTypeAttributeKeys.PixelAspectRatio, PackLong(1, 1));

            using var mediaTypeOut = new MediaType();
            mediaTypeOut.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            mediaTypeOut.Set(MediaTypeAttributeKeys.Subtype, outputPixelFormat);
            mediaTypeOut.Set(MediaTypeAttributeKeys.FrameSize, PackLong(width, height));
            mediaTypeOut.Set(MediaTypeAttributeKeys.FrameRate, PackLong(fps, 1));
            mediaTypeOut.Set(MediaTypeAttributeKeys.PixelAspectRatio, PackLong(1, 1));

            try
            {
                transform.SetInputType(0, mediaTypeIn, 0);
                transform.SetOutputType(0, mediaTypeOut, 0);
                return true;
            }
            catch
            {
                try
                {
                    transform.SetOutputType(0, mediaTypeOut, 0);
                    transform.SetInputType(0, mediaTypeIn, 0);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        static long PackLong(int left, int right)
        {
            return ((long)left << 32) | (uint)right;
        }

        static string GetGpuName()
        {
            try
            {
                using var factory = new Factory1();
                var adapter = factory.Adapters1.FirstOrDefault();
                return adapter?.Description.Description ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
