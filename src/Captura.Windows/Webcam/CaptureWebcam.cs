using System;
using System.Drawing;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.Misc;

namespace Captura.Webcam
{
    /// <summary>
    /// Modern MediaFoundation-based webcam capture implementation
    /// </summary>
    class CaptureWebcam : IDisposable
    {
        #region Fields
        readonly Filter _videoDevice;
        readonly IntPtr _previewWindow;
        readonly DummyForm _form;
        readonly Action _onClick;

        IMFMediaSource _mediaSource;
        IMFSourceReader _sourceReader;
        IMFActivate _videoDeviceActivate;
        
        Size _videoSize;
        bool _isInitialized;
        readonly object _lock = new object();
        
        byte[] _frameBuffer;
        int _stride;
        #endregion

        public CaptureWebcam(Filter VideoDevice, Action OnClick, IntPtr PreviewWindow)
        {
            _videoDevice = VideoDevice ?? throw new ArgumentNullException(nameof(VideoDevice));
            _onClick = OnClick;

            // Create dummy form for preview
            _form = new DummyForm();
            _form.Show();
            _form.Click += (s, e) => OnClick?.Invoke();

            _previewWindow = PreviewWindow != IntPtr.Zero ? PreviewWindow : _form.Handle;

            // Initialize MediaFoundation
            var hr = MFExterns.MFStartup(MF_VERSION.MF_SDK_VERSION, MFStartup.Full);
            MFError.ThrowExceptionForHR(hr);

            InitializeDevice();
        }

        public Size Size
        {
            get
            {
                lock (_lock)
                {
                    return _videoSize;
                }
            }
        }

        #region Initialization

        void InitializeDevice()
        {
            try
            {
                lock (_lock)
                {
                    // Get the device activate object from moniker
                    _videoDeviceActivate = GetDeviceActivate(_videoDevice.MonikerString);
                    
                    if (_videoDeviceActivate == null)
                        throw new InvalidOperationException("Could not find video device");

                    // Create the media source from the activate object
                    object sourceObject;
                    var hr = _videoDeviceActivate.ActivateObject(typeof(IMFMediaSource).GUID, out sourceObject);
                    MFError.ThrowExceptionForHR(hr);

                    _mediaSource = (IMFMediaSource)sourceObject;

                    // Create source reader for frame capture
                    CreateSourceReader();

                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Cleanup();
                throw new InvalidOperationException($"Failed to initialize webcam: {ex.Message}", ex);
            }
        }

        IMFActivate GetDeviceActivate(string devicePath)
        {
            IMFAttributes attributes;
            var hr = MFExterns.MFCreateAttributes(out attributes, 1);
            MFError.ThrowExceptionForHR(hr);

            try
            {
                // Set attribute to enumerate video capture devices
                hr = attributes.SetGUID(
                    MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    CLSID.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
                MFError.ThrowExceptionForHR(hr);

                IMFActivate[] devices;
                int count;
                hr = MFExterns.MFEnumDeviceSources(attributes, out devices, out count);
                MFError.ThrowExceptionForHR(hr);

                // Find device matching our path
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        string symbolicLink;
                        int linkLength;
                        hr = devices[i].GetString(
                            MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                            out symbolicLink,
                            out linkLength);

                        if (hr == 0 && devicePath.Contains(symbolicLink))
                        {
                            // Found our device, release others
                            for (int j = 0; j < count; j++)
                            {
                                if (i != j && devices[j] != null)
                                    Marshal.ReleaseComObject(devices[j]);
                            }
                            return devices[i];
                        }
                    }
                    catch
                    {
                        // Continue to next device
                    }
                }

                // If exact match not found, try to use first available device
                // (devicePath might be from DirectShow, symbolicLink is from MediaFoundation)
                if (count > 0)
                {
                    var firstDevice = devices[0];
                    for (int j = 1; j < count; j++)
                    {
                        if (devices[j] != null)
                            Marshal.ReleaseComObject(devices[j]);
                    }
                    return firstDevice;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }

            return null;
        }

        void CreateSourceReader()
        {
            // Create attributes for source reader
            IMFAttributes attributes;
            var hr = MFExterns.MFCreateAttributes(out attributes, 2);
            MFError.ThrowExceptionForHR(hr);

            try
            {
                // Enable video processing (for format conversion)
                hr = attributes.SetUINT32(MFAttributesClsid.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING, 1);
                MFError.ThrowExceptionForHR(hr);

                // Create source reader
                hr = MFExterns.MFCreateSourceReaderFromMediaSource(_mediaSource, attributes, out _sourceReader);
                MFError.ThrowExceptionForHR(hr);

                // Configure the source reader to give us RGB32 frames
                ConfigureSourceReader();
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }
        }

        void ConfigureSourceReader()
        {
            // Create media type for RGB32
            IMFMediaType mediaType;
            var hr = MFExterns.MFCreateMediaType(out mediaType);
            MFError.ThrowExceptionForHR(hr);

            try
            {
                // Set video format
                hr = mediaType.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video);
                MFError.ThrowExceptionForHR(hr);

                // Request RGB32 output format (BGRA in memory order)
                hr = mediaType.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB32);
                MFError.ThrowExceptionForHR(hr);

                // Set this as the current media type
                hr = _sourceReader.SetCurrentMediaType(MF_SOURCE_READER.FirstVideoStream, null, mediaType);
                MFError.ThrowExceptionForHR(hr);

                // Get the actual media type that was set (with frame size info)
                IMFMediaType actualMediaType;
                hr = _sourceReader.GetCurrentMediaType(MF_SOURCE_READER.FirstVideoStream, out actualMediaType);
                MFError.ThrowExceptionForHR(hr);

                try
                {
                    // Get frame size
                    long frameSize;
                    hr = actualMediaType.GetUINT64(MFAttributesClsid.MF_MT_FRAME_SIZE, out frameSize);
                    MFError.ThrowExceptionForHR(hr);

                    int width = (int)(frameSize >> 32);
                    int height = (int)(frameSize & 0xFFFFFFFF);
                    _videoSize = new Size(width, height);

                    // Calculate stride
                    int stride;
                    hr = MFExterns.MFGetStrideForBitmapInfoHeader((int)MFMediaType.RGB32.Data1, width, out stride);
                    if (hr >= 0)
                    {
                        _stride = Math.Abs(stride);
                    }
                    else
                    {
                        _stride = width * 4; // 4 bytes per pixel for RGB32
                    }

                    // Allocate frame buffer
                    _frameBuffer = new byte[_stride * height];
                }
                finally
                {
                    Marshal.ReleaseComObject(actualMediaType);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(mediaType);
            }
        }

        #endregion

        #region Public Methods

        public void StartPreview()
        {
            lock (_lock)
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Device not initialized");

                // MediaFoundation source reader doesn't require explicit preview start
                // Just ensure we can read a frame
                try
                {
                    IMFSample sample;
                    int streamIndex;
                    MF_SOURCE_READER_FLAG flags;
                    long timestamp;

                    var hr = _sourceReader.ReadSample(
                        MF_SOURCE_READER.FirstVideoStream,
                        MF_SOURCE_READER_CONTROL_FLAG.None,
                        out streamIndex,
                        out flags,
                        out timestamp,
                        out sample);

                    if (sample != null)
                        Marshal.ReleaseComObject(sample);
                }
                catch
                {
                    // Ignore errors on first frame read
                }
            }
        }

        public void StopPreview()
        {
            // MediaFoundation automatically handles stopping when reader is released
        }

        public void OnPreviewWindowResize(int X, int Y, int Width, int Height)
        {
            // Preview positioning is handled by the parent control
            // This method is kept for interface compatibility
        }

        public Captura.IBitmapImage GetFrame(Captura.IBitmapLoader BitmapLoader)
        {
            lock (_lock)
            {
                if (!_isInitialized || _sourceReader == null)
                    return null;

                try
                {
                    IMFSample sample = null;
                    int streamIndex;
                    MF_SOURCE_READER_FLAG flags;
                    long timestamp;

                    var hr = _sourceReader.ReadSample(
                        MF_SOURCE_READER.FirstVideoStream,
                        MF_SOURCE_READER_CONTROL_FLAG.None,
                        out streamIndex,
                        out flags,
                        out timestamp,
                        out sample);

                    if (hr < 0 || sample == null)
                        return null;

                    try
                    {
                        // Get the media buffer from the sample
                        IMFMediaBuffer buffer;
                        hr = sample.ConvertToContiguousBuffer(out buffer);
                        if (hr < 0)
                        {
                            // Try getting buffer directly
                            hr = sample.GetBufferByIndex(0, out buffer);
                        }
                        
                        if (hr < 0 || buffer == null)
                            return null;

                        try
                        {
                            // Lock the buffer and copy data
                            IntPtr pData;
                            int maxLength, currentLength;

                            hr = buffer.Lock(out pData, out maxLength, out currentLength);
                            if (hr < 0)
                                return null;

                            try
                            {
                                // Copy frame data to our buffer
                                if (currentLength > 0 && currentLength <= _frameBuffer.Length)
                                {
                                    Marshal.Copy(pData, _frameBuffer, 0, currentLength);

                                    // Pin the buffer and create bitmap
                                    var handle = GCHandle.Alloc(_frameBuffer, GCHandleType.Pinned);
                                    try
                                    {
                                        var address = handle.AddrOfPinnedObject();
                                        
                                        // RGB32 from MediaFoundation is actually BGRA32 in memory
                                        // Need to flip vertically as MF gives bottom-up bitmaps
                                        return BitmapLoader.CreateBitmapBgr32(_videoSize, address + (_videoSize.Height - 1) * _stride, -_stride);
                                    }
                                    finally
                                    {
                                        handle.Free();
                                    }
                                }
                            }
                            finally
                            {
                                buffer.Unlock();
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(buffer);
                        }
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(sample);
                    }
                }
                catch
                {
                    return null;
                }

                return null;
            }
        }

        #endregion

        #region Cleanup

        void Cleanup()
        {
            lock (_lock)
            {
                _isInitialized = false;

                if (_sourceReader != null)
                {
                    try { Marshal.ReleaseComObject(_sourceReader); }
                    catch { }
                    _sourceReader = null;
                }

                if (_mediaSource != null)
                {
                    try
                    {
                        _mediaSource.Shutdown();
                        Marshal.ReleaseComObject(_mediaSource);
                    }
                    catch { }
                    _mediaSource = null;
                }

                if (_videoDeviceActivate != null)
                {
                    try
                    {
                        _videoDeviceActivate.ShutdownObject();
                        Marshal.ReleaseComObject(_videoDeviceActivate);
                    }
                    catch { }
                    _videoDeviceActivate = null;
                }
            }
        }

        public void Dispose()
        {
            Cleanup();

            try
            {
                _form?.Dispose();
            }
            catch { }

            try
            {
                MFExterns.MFShutdown();
            }
            catch
            {
                // Ignore shutdown errors
            }

            _frameBuffer = null;
        }

        #endregion
    }
}
