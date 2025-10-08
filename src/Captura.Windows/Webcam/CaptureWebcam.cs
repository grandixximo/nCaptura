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
            int hr;
            
            // Set the media type for RGB32 (preferred format)
            var mediaType = new AMMediaType
            {
                majorType = MediaType.Video,
                subType = MediaSubType.RGB32,
                formatType = FormatType.VideoInfo
            };

            hr = _sampleGrabber.SetMediaType(mediaType);
            DsUtils.FreeAMMediaType(mediaType);
            
            // If RGB32 fails, try without specifying format (let DirectShow choose)
            if (hr < 0)
            {
                mediaType = new AMMediaType
                {
                    majorType = MediaType.Video
                };
                
                hr = _sampleGrabber.SetMediaType(mediaType);
                DsUtils.FreeAMMediaType(mediaType);
                DsError.ThrowExceptionForHR(hr);
            }

            // Configure grabber to buffer samples
            hr = _sampleGrabber.SetBufferSamples(true);
            DsError.ThrowExceptionForHR(hr);

            hr = _sampleGrabber.SetOneShot(false);
            DsError.ThrowExceptionForHR(hr);

            // Don't need the callback, we'll use GetCurrentBuffer
            hr = _sampleGrabber.SetCallback(null, 0);
            DsError.ThrowExceptionForHR(hr);
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

            // Try to render preview pin first, fall back to capture pin
            var previewPin = DsFindPin.ByCategory(_videoDeviceFilter, PinCategory.Preview, 0);
            
            if (previewPin != null)
            {
                hr = _captureGraphBuilder.RenderStream(PinCategory.Preview, MediaType.Video, 
                    _videoDeviceFilter, _sampleGrabberFilter, null);
                
                Marshal.ReleaseComObject(previewPin);
            }
            else
            {
                // No preview pin, use capture pin (common for virtual cameras)
                hr = _captureGraphBuilder.RenderStream(PinCategory.Capture, MediaType.Video,
                    _videoDeviceFilter, _sampleGrabberFilter, null);
            }

            if (hr < 0)
            {
                var error = DsError.GetErrorText(hr);
                throw new InvalidOperationException($"Failed to render video stream (HR: 0x{hr:X8}): {error}. The camera may not support RGB32 format or may be in use.");
            }

            // Get the actual media type that was connected
            var mediaType = new AMMediaType();
            hr = _sampleGrabber.GetConnectedMediaType(mediaType);
            DsError.ThrowExceptionForHR(hr);

            try
            {
                if (mediaType.formatType == FormatType.VideoInfo && mediaType.formatPtr != IntPtr.Zero)
                {
                    _videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                    _videoSize = new Size(_videoInfoHeader.BmiHeader.Width, Math.Abs(_videoInfoHeader.BmiHeader.Height));
                    
                    // Calculate stride based on bit depth (typically 32 bits for RGB32, but could be different)
                    var bitsPerPixel = _videoInfoHeader.BmiHeader.BitCount;
                    _stride = (_videoSize.Width * bitsPerPixel + 7) / 8;
                    
                    // Align stride to 4-byte boundary (standard for DIBs)
                    _stride = (_stride + 3) & ~3;
                    
                    _frameBuffer = new byte[_stride * _videoSize.Height];
                }
                else if (mediaType.formatType == FormatType.VideoInfo2 && mediaType.formatPtr != IntPtr.Zero)
                {
                    var videoInfoHeader2 = (VideoInfoHeader2)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader2));
                    _videoSize = new Size(videoInfoHeader2.BmiHeader.Width, Math.Abs(videoInfoHeader2.BmiHeader.Height));
                    
                    // Calculate stride based on bit depth
                    var bitsPerPixel = videoInfoHeader2.BmiHeader.BitCount;
                    _stride = (_videoSize.Width * bitsPerPixel + 7) / 8;
                    _stride = (_stride + 3) & ~3;
                    
                    _frameBuffer = new byte[_stride * _videoSize.Height];
                    
                    // Create a VideoInfoHeader for compatibility
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
                    if (hr >= 0)
                    {
                        hr = _videoWindow.put_MessageDrain(_form.Handle);
                        if (hr >= 0)
                        {
                            // Set window style for child window
                            hr = _videoWindow.put_WindowStyle(WindowStyle.Child | WindowStyle.ClipChildren | WindowStyle.ClipSiblings);
                        }
                    }
                    
                    // If any setup failed, video window won't work but frame capture still can
                    // Virtual cameras (like OBS) often don't support video windows
                    if (hr < 0)
                    {
                        _videoWindow = null; // Mark as unavailable
                    }
                }
                catch
                {
                    // Video window setup failed - this is OK for virtual cameras
                    // Frame capture can still work without the preview window
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
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        #endregion

        #region Frame Capture

        public Captura.IBitmapImage GetFrame(Captura.IBitmapLoader BitmapLoader)
        {
            lock (_lock)
            {
                if (!_isRunning || _sampleGrabber == null || _frameBuffer == null)
                    return null;

                try
                {
                    // Get buffer size
                    int bufferSize = 0;
                    var hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                    
                    if (hr < 0 || bufferSize <= 0)
                        return null;

                    // Ensure our buffer is large enough
                    if (_frameBuffer.Length < bufferSize)
                        _frameBuffer = new byte[bufferSize];

                    // Pin the buffer and get the data
                    var handle = GCHandle.Alloc(_frameBuffer, GCHandleType.Pinned);
                    try
                    {
                        var ptr = handle.AddrOfPinnedObject();
                        hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, ptr);
                        
                        if (hr < 0)
                            return null;

                        // DirectShow gives us bottom-up bitmaps, so we need to flip
                        // Move to the last line and use negative stride
                        var dataPtr = ptr + (_videoSize.Height - 1) * _stride;
                        
                        return BitmapLoader.CreateBitmapBgr32(_videoSize, dataPtr, -_stride);
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
