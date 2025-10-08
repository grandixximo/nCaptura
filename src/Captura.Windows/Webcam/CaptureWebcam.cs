using System;
using System.Drawing;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace Captura.Webcam
{
    /// <summary>
    /// Clean DirectShow-based webcam capture implementation
    /// </summary>
    class CaptureWebcam : ISampleGrabberCB, IDisposable
    {
        #region Fields
        readonly Filter _videoDevice;
        readonly IntPtr _previewWindow;
        readonly DummyForm _form;
        readonly Action _onClick;
        readonly object _lock = new object();

        // DirectShow interfaces
        IGraphBuilder _graphBuilder;
        ICaptureGraphBuilder2 _captureGraphBuilder;
        IBaseFilter _videoDeviceFilter;
        IBaseFilter _sampleGrabberFilter;
        ISampleGrabber _sampleGrabber;
        IMediaControl _mediaControl;
        IVideoWindow _videoWindow;

        // Video properties
        Size _videoSize = Size.Empty;
        int _stride;
        byte[] _frameBuffer;
        VideoInfoHeader _videoInfoHeader;
        Guid _negotiatedSubType;
        
        bool _isRunning;
        bool _isDisposed;
        #endregion

        public CaptureWebcam(Filter VideoDevice, Action OnClick, IntPtr PreviewWindow)
        {
            _videoDevice = VideoDevice ?? throw new ArgumentNullException(nameof(VideoDevice));
            _onClick = OnClick;

            // Create dummy form for handling clicks
            _form = new DummyForm();
            _form.Show();
            _form.Click += (s, e) => OnClick?.Invoke();

            _previewWindow = PreviewWindow != IntPtr.Zero ? PreviewWindow : _form.Handle;

            BuildGraph();
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

        #region Graph Building

        void BuildGraph()
        {
            int hr;

            try
            {
                // Create the filter graph
                _graphBuilder = (IGraphBuilder)new FilterGraph();

                // Create the capture graph builder
                _captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

                // Attach the filter graph to the capture graph
                hr = _captureGraphBuilder.SetFiltergraph(_graphBuilder);
                DsError.ThrowExceptionForHR(hr);

                // Add the video device
                _videoDeviceFilter = CreateVideoDeviceFilter();
                hr = _graphBuilder.AddFilter(_videoDeviceFilter, "Video Capture");
                DsError.ThrowExceptionForHR(hr);

                // Add and configure the sample grabber
                _sampleGrabber = (ISampleGrabber)new SampleGrabber();
                _sampleGrabberFilter = (IBaseFilter)_sampleGrabber;

                ConfigureSampleGrabber();

                hr = _graphBuilder.AddFilter(_sampleGrabberFilter, "Sample Grabber");
                DsError.ThrowExceptionForHR(hr);

                // Get the media control interface
                _mediaControl = (IMediaControl)_graphBuilder;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        IBaseFilter CreateVideoDeviceFilter()
        {
            object source = null;
            
            try
            {
                source = Marshal.BindToMoniker(_videoDevice.MonikerString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot bind to device: {_videoDevice.Name}", ex);
            }
            
            if (source == null)
            {
                throw new InvalidOperationException($"Cannot bind to device: {_videoDevice.Name}");
            }

            return (IBaseFilter)source;
        }

        void ConfigureSampleGrabber()
        {
            var mediaType = new AMMediaType
            {
                majorType = MediaType.Video,
                subType = MediaSubType.RGB24
            };

            var hr = _sampleGrabber.SetMediaType(mediaType);
            DsUtils.FreeAMMediaType(mediaType);
            
            if (hr < 0)
            {
                var fallback = new AMMediaType { majorType = MediaType.Video };
                hr = _sampleGrabber.SetMediaType(fallback);
                DsUtils.FreeAMMediaType(fallback);
                
                if (hr < 0)
                    throw new InvalidOperationException($"Failed to configure sample grabber (HR: 0x{hr:X8})");
            }

            hr = _sampleGrabber.SetBufferSamples(true);
            if (hr < 0) throw new InvalidOperationException("Failed to set buffer samples");

            hr = _sampleGrabber.SetOneShot(false);
            if (hr < 0) throw new InvalidOperationException("Failed to set one shot mode");

            hr = _sampleGrabber.SetCallback(null, 0);
            if (hr < 0) throw new InvalidOperationException("Failed to set callback");
        }

        #endregion

        #region Preview Management

        public void StartPreview()
        {
            lock (_lock)
            {
                if (_isRunning || _isDisposed)
                    return;

                try
                {
                    RenderPreview();
                    
                    // Start the graph
                    var hr = _mediaControl.Run();
                    if (hr < 0)
                    {
                        var error = DsError.GetErrorText(hr);
                        throw new InvalidOperationException($"Failed to start media control (HR: 0x{hr:X8}): {error}");
                    }

                    _isRunning = true;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to start preview: {ex.Message}", ex);
                }
            }
        }

        void RenderPreview()
        {
            int hr;

            // Try multiple approaches to connect the camera to the sample grabber
            
            // Approach 1: Try preview pin with explicit media type
            var previewPin = DsFindPin.ByCategory(_videoDeviceFilter, PinCategory.Preview, 0);
            
            if (previewPin != null)
            {
                hr = _captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, 
                    _videoDeviceFilter, _sampleGrabberFilter, null);
                Marshal.ReleaseComObject(previewPin);
                
                if (hr >= 0)
                    goto success; // Preview pin worked
            }

            // Approach 2: Try capture pin with explicit media type
            hr = _captureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video,
                _videoDeviceFilter, _sampleGrabberFilter, null);
            
            if (hr >= 0)
                goto success;

            // Approach 3: Try without specifying category (let DirectShow figure it out)
            hr = _captureGraphBuilder.RenderStream(null, MediaType.Video,
                _videoDeviceFilter, _sampleGrabberFilter, null);
            
            if (hr >= 0)
                goto success;

            // Approach 4: Try without any media type specification
            hr = _captureGraphBuilder.RenderStream(null, null,
                _videoDeviceFilter, _sampleGrabberFilter, null);
            
            if (hr >= 0)
                goto success;

            // All approaches failed
            var error = DsError.GetErrorText(hr);
            throw new InvalidOperationException(
                $"Failed to connect camera (HR: 0x{hr:X8}): {error}.\n\n" +
                $"This camera may not be compatible with DirectShow capture.\n" +
                $"Camera: {_videoDevice.Name}");

            success:
            ; // Continue with getting media type

            // Get the actual media type that was connected
            var mediaType = new AMMediaType();
            hr = _sampleGrabber.GetConnectedMediaType(mediaType);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                _negotiatedSubType = mediaType.subType;
                
                var subTypeStr = $"{_negotiatedSubType:B}";
                System.Diagnostics.Debug.WriteLine($"Negotiated format: {subTypeStr}");

                if (mediaType.formatType == FormatType.VideoInfo && mediaType.formatPtr != IntPtr.Zero)
                {
                    _videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                    _videoSize = new Size(_videoInfoHeader.BmiHeader.Width, Math.Abs(_videoInfoHeader.BmiHeader.Height));
                    
                    var bitsPerPixel = _videoInfoHeader.BmiHeader.BitCount;
                    _stride = (_videoSize.Width * bitsPerPixel + 7) / 8;
                    _stride = (_stride + 3) & ~3;
                    
                    _frameBuffer = new byte[_stride * _videoSize.Height];
                }
                else if (mediaType.formatType == FormatType.VideoInfo2 && mediaType.formatPtr != IntPtr.Zero)
                {
                    var videoInfoHeader2 = (VideoInfoHeader2)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader2));
                    _videoSize = new Size(videoInfoHeader2.BmiHeader.Width, Math.Abs(videoInfoHeader2.BmiHeader.Height));
                    
                    var bitsPerPixel = videoInfoHeader2.BmiHeader.BitCount;
                    _stride = (_videoSize.Width * bitsPerPixel + 7) / 8;
                    _stride = (_stride + 3) & ~3;
                    
                    _frameBuffer = new byte[_stride * _videoSize.Height];
                    
                    _videoInfoHeader = new VideoInfoHeader
                    {
                        BmiHeader = videoInfoHeader2.BmiHeader
                    };
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported video format: {mediaType.formatType}");
                }
            }
            finally
            {
                DsUtils.FreeAMMediaType(mediaType);
            }

            // Setup video window for preview
            SetupVideoWindow();
        }

        void SetupVideoWindow()
        {
            // Get the video window interface
            _videoWindow = _graphBuilder as IVideoWindow;
            
            if (_videoWindow != null)
            {
                try
                {
                    var hr = _videoWindow.put_Owner(_previewWindow);
                    if (hr < 0)
                    {
                        _videoWindow = null;
                        return;
                    }

                    hr = _videoWindow.put_MessageDrain(_form.Handle);
                    if (hr < 0)
                    {
                        _videoWindow = null;
                        return;
                    }

                    // Set window style for child window
                    hr = _videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipChildren | WindowStyle.ClipSiblings);
                    if (hr < 0)
                    {
                        _videoWindow = null;
                        return;
                    }

                    // IMPORTANT: Make the video window visible!
                    hr = _videoWindow.put_Visible(OABool.True);
                    if (hr < 0)
                    {
                        _videoWindow = null;
                        return;
                    }
                }
                catch
                {
                    // Video window setup failed - this is OK for virtual cameras
                    _videoWindow = null;
                }
            }
        }

        public void StopPreview()
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return;

                try
                {
                    _mediaControl?.Stop();
                    
                    if (_videoWindow != null)
                    {
                        _videoWindow.put_Visible(OABool.False);
                        _videoWindow.put_Owner(IntPtr.Zero);
                    }

                    _isRunning = false;
                }
                catch
                {
                    // Ignore errors when stopping
                }
            }
        }

        public void OnPreviewWindowResize(int X, int Y, int Width, int Height)
        {
            if (_videoWindow != null && _isRunning)
            {
                try
                {
                    _videoWindow.SetWindowPosition(X, Y, Width, Height);
                    
                    // Ensure window is visible
                    _videoWindow.put_Visible(OABool.True);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        #endregion

        #region Frame Capture

        static readonly Guid MEDIASUBTYPE_YUY2 = new Guid(0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        static readonly Guid MEDIASUBTYPE_UYVY = new Guid(0x59565955, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

        static void ConvertYuy2ToBgr32(byte[] src, byte[] dst, int width, int height, int srcStride)
        {
            for (var y = 0; y < height; y++)
            {
                var srcIdx = y * srcStride;
                var dstIdx = y * width * 4;
                
                for (var x = 0; x < width; x += 2)
                {
                    var y0 = src[srcIdx];
                    var u = src[srcIdx + 1];
                    var y1 = src[srcIdx + 2];
                    var v = src[srcIdx + 3];
                    
                    var c = 298;
                    var d = u - 128;
                    var e = v - 128;
                    
                    var r0 = (c * (y0 - 16) + 409 * e + 128) >> 8;
                    var g0 = (c * (y0 - 16) - 100 * d - 208 * e + 128) >> 8;
                    var b0 = (c * (y0 - 16) + 516 * d + 128) >> 8;
                    
                    dst[dstIdx] = (byte)(b0 < 0 ? 0 : b0 > 255 ? 255 : b0);
                    dst[dstIdx + 1] = (byte)(g0 < 0 ? 0 : g0 > 255 ? 255 : g0);
                    dst[dstIdx + 2] = (byte)(r0 < 0 ? 0 : r0 > 255 ? 255 : r0);
                    dst[dstIdx + 3] = 255;
                    
                    if (x + 1 < width)
                    {
                        var r1 = (c * (y1 - 16) + 409 * e + 128) >> 8;
                        var g1 = (c * (y1 - 16) - 100 * d - 208 * e + 128) >> 8;
                        var b1 = (c * (y1 - 16) + 516 * d + 128) >> 8;
                        
                        dst[dstIdx + 4] = (byte)(b1 < 0 ? 0 : b1 > 255 ? 255 : b1);
                        dst[dstIdx + 5] = (byte)(g1 < 0 ? 0 : g1 > 255 ? 255 : g1);
                        dst[dstIdx + 6] = (byte)(r1 < 0 ? 0 : r1 > 255 ? 255 : r1);
                        dst[dstIdx + 7] = 255;
                    }
                    
                    srcIdx += 4;
                    dstIdx += 8;
                }
            }
        }

        static void ConvertUyvyToBgr32(byte[] src, byte[] dst, int width, int height, int srcStride)
        {
            for (var y = 0; y < height; y++)
            {
                var srcIdx = y * srcStride;
                var dstIdx = y * width * 4;
                
                for (var x = 0; x < width; x += 2)
                {
                    var u = src[srcIdx];
                    var y0 = src[srcIdx + 1];
                    var v = src[srcIdx + 2];
                    var y1 = src[srcIdx + 3];
                    
                    var c = 298;
                    var d = u - 128;
                    var e = v - 128;
                    
                    var r0 = (c * (y0 - 16) + 409 * e + 128) >> 8;
                    var g0 = (c * (y0 - 16) - 100 * d - 208 * e + 128) >> 8;
                    var b0 = (c * (y0 - 16) + 516 * d + 128) >> 8;
                    
                    dst[dstIdx] = (byte)(b0 < 0 ? 0 : b0 > 255 ? 255 : b0);
                    dst[dstIdx + 1] = (byte)(g0 < 0 ? 0 : g0 > 255 ? 255 : g0);
                    dst[dstIdx + 2] = (byte)(r0 < 0 ? 0 : r0 > 255 ? 255 : r0);
                    dst[dstIdx + 3] = 255;
                    
                    if (x + 1 < width)
                    {
                        var r1 = (c * (y1 - 16) + 409 * e + 128) >> 8;
                        var g1 = (c * (y1 - 16) - 100 * d - 208 * e + 128) >> 8;
                        var b1 = (c * (y1 - 16) + 516 * d + 128) >> 8;
                        
                        dst[dstIdx + 4] = (byte)(b1 < 0 ? 0 : b1 > 255 ? 255 : b1);
                        dst[dstIdx + 5] = (byte)(g1 < 0 ? 0 : g1 > 255 ? 255 : g1);
                        dst[dstIdx + 6] = (byte)(r1 < 0 ? 0 : r1 > 255 ? 255 : r1);
                        dst[dstIdx + 7] = 255;
                    }
                    
                    srcIdx += 4;
                    dstIdx += 8;
                }
            }
        }

        public Captura.IBitmapImage GetFrame(Captura.IBitmapLoader BitmapLoader)
        {
            lock (_lock)
            {
                if (!_isRunning || _sampleGrabber == null || _frameBuffer == null)
                    return null;

                try
                {
                    var bufferSize = 0;
                    var hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                    
                    if (hr < 0 || bufferSize <= 0)
                        return null;

                    if (_frameBuffer.Length < bufferSize)
                        _frameBuffer = new byte[bufferSize];

                    var handle = GCHandle.Alloc(_frameBuffer, GCHandleType.Pinned);
                    try
                    {
                        var ptr = handle.AddrOfPinnedObject();
                        hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, ptr);
                        
                        if (hr < 0)
                            return null;

                        byte[] rgbData;
                        var rgbStride = _videoSize.Width * 4;

                        if (_negotiatedSubType == MEDIASUBTYPE_YUY2)
                        {
                            rgbData = new byte[_videoSize.Width * _videoSize.Height * 4];
                            ConvertYuy2ToBgr32(_frameBuffer, rgbData, _videoSize.Width, _videoSize.Height, _stride);
                        }
                        else if (_negotiatedSubType == MEDIASUBTYPE_UYVY)
                        {
                            rgbData = new byte[_videoSize.Width * _videoSize.Height * 4];
                            ConvertUyvyToBgr32(_frameBuffer, rgbData, _videoSize.Width, _videoSize.Height, _stride);
                        }
                        else
                        {
                            rgbData = _frameBuffer;
                            rgbStride = _stride;
                        }

                        var rgbHandle = GCHandle.Alloc(rgbData, GCHandleType.Pinned);
                        try
                        {
                            var dataPtr = rgbHandle.AddrOfPinnedObject() + (_videoSize.Height - 1) * rgbStride;
                            return BitmapLoader.CreateBitmapBgr32(_videoSize, dataPtr, -rgbStride);
                        }
                        finally
                        {
                            rgbHandle.Free();
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        #endregion

        #region ISampleGrabberCB Implementation

        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            return 0;
        }

        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            return 0;
        }

        #endregion

        #region Cleanup

        void Cleanup()
        {
            lock (_lock)
            {
                _isRunning = false;

                // Stop the graph
                try { _mediaControl?.Stop(); } catch { }

                // Release video window
                if (_videoWindow != null)
                {
                    try
                    {
                        _videoWindow.put_Visible(OABool.False);
                        _videoWindow.put_Owner(IntPtr.Zero);
                    }
                    catch { }
                    _videoWindow = null;
                }

                // Release interfaces
                _mediaControl = null;

                if (_sampleGrabber != null)
                {
                    try { Marshal.ReleaseComObject(_sampleGrabber); } catch { }
                    _sampleGrabber = null;
                }

                if (_sampleGrabberFilter != null)
                {
                    try { Marshal.ReleaseComObject(_sampleGrabberFilter); } catch { }
                    _sampleGrabberFilter = null;
                }

                if (_videoDeviceFilter != null)
                {
                    try { Marshal.ReleaseComObject(_videoDeviceFilter); } catch { }
                    _videoDeviceFilter = null;
                }

                if (_captureGraphBuilder != null)
                {
                    try { Marshal.ReleaseComObject(_captureGraphBuilder); } catch { }
                    _captureGraphBuilder = null;
                }

                if (_graphBuilder != null)
                {
                    try { Marshal.ReleaseComObject(_graphBuilder); } catch { }
                    _graphBuilder = null;
                }

                _frameBuffer = null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Cleanup();

            try
            {
                _form?.Dispose();
            }
            catch { }
        }

        #endregion
    }
}
