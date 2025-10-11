using System;
using System.Drawing;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace Captura.Webcam
{
    /// <summary>
    /// Clean DirectShow-based webcam capture implementation
    /// </summary>
    public class CaptureWebcam : ISampleGrabberCB, IDisposable
    {
        #region Fields
        readonly Filter _videoDevice;
        readonly IntPtr _previewWindow;
        IntPtr _currentOwner;
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
        int _bitsPerPixel;
        int _compression;
        byte[] _frameBuffer;
        byte[] _convertedBuffer;
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
            _currentOwner = _previewWindow;

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

        public string GetCameraProperties()
        {
            lock (_lock)
            {
                var sb = new System.Text.StringBuilder();
                
                sb.AppendLine("=== CAMERA DEVICE INFORMATION ===");
                sb.AppendLine($"Device Name: {_videoDevice.Name}");
                sb.AppendLine($"Moniker: {_videoDevice.MonikerString}");
                sb.AppendLine();
                
                sb.AppendLine("=== CURRENT VIDEO FORMAT ===");
                sb.AppendLine($"Resolution: {_videoSize.Width} x {_videoSize.Height}");
                sb.AppendLine($"Running: {_isRunning}");
                sb.AppendLine();
                
                if (_videoInfoHeader.BmiHeader.Size > 0)
                {
                    sb.AppendLine("=== BITMAP INFO HEADER ===");
                    sb.AppendLine($"Width: {_videoInfoHeader.BmiHeader.Width}");
                    sb.AppendLine($"Height: {_videoInfoHeader.BmiHeader.Height}");
                    sb.AppendLine($"BitCount: {_videoInfoHeader.BmiHeader.BitCount} bits per pixel");
                    
                    var compression = _videoInfoHeader.BmiHeader.Compression;
                    sb.AppendLine($"Compression: 0x{compression:X8}");
                    
                    // Decode compression format
                    if (compression == 0)
                    {
                        sb.AppendLine($"Compression Type: BI_RGB (Uncompressed)");
                    }
                    else
                    {
                        var compressionBytes = System.BitConverter.GetBytes(compression);
                        var compressionStr = System.Text.Encoding.ASCII.GetString(compressionBytes).TrimEnd('\0');
                        sb.AppendLine($"Compression Type: {compressionStr}");
                        
                        if (compressionStr.Contains("MJPG") || compressionStr.Contains("JPEG"))
                        {
                            sb.AppendLine("*** INFO: Camera is using MJPEG compression.");
                            sb.AppendLine("*** MJPEG format is supported and will be automatically decompressed.");
                        }
                    }
                    
                    sb.AppendLine($"ImageSize: {_videoInfoHeader.BmiHeader.ImageSize} bytes");
                    sb.AppendLine($"XPelsPerMeter: {_videoInfoHeader.BmiHeader.XPelsPerMeter}");
                    sb.AppendLine($"YPelsPerMeter: {_videoInfoHeader.BmiHeader.YPelsPerMeter}");
                    sb.AppendLine($"ClrUsed: {_videoInfoHeader.BmiHeader.ClrUsed}");
                    sb.AppendLine($"ClrImportant: {_videoInfoHeader.BmiHeader.ClrImportant}");
                    sb.AppendLine();
                    
                    sb.AppendLine("=== CALCULATED VALUES ===");
                    sb.AppendLine($"Stride: {_stride} bytes");
                    sb.AppendLine($"Frame Buffer Size: {_frameBuffer?.Length ?? 0} bytes");
                    sb.AppendLine($"Expected Frame Size: {_stride * _videoSize.Height} bytes");
                    sb.AppendLine();
                }
                
                try
                {
                    if (_sampleGrabber != null && _isRunning)
                    {
                        sb.AppendLine("=== SAMPLE GRABBER INFO ===");
                        
                        var mediaType = new AMMediaType();
                        var hr = _sampleGrabber.GetConnectedMediaType(mediaType);
                        
                        if (hr >= 0)
                        {
                            try
                            {
                                sb.AppendLine($"Major Type: {mediaType.majorType}");
                                sb.AppendLine($"Sub Type: {mediaType.subType}");
                                sb.AppendLine($"Format Type: {mediaType.formatType}");
                                sb.AppendLine($"Fixed Size Samples: {mediaType.fixedSizeSamples}");
                                sb.AppendLine($"Temporal Compression: {mediaType.temporalCompression}");
                                sb.AppendLine($"Sample Size: {mediaType.sampleSize} bytes");
                                sb.AppendLine();
                                
                                if (mediaType.formatPtr != IntPtr.Zero)
                                {
                                    if (mediaType.formatType == FormatType.VideoInfo)
                                    {
                                        var vih = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                                        sb.AppendLine("=== VIDEO INFO HEADER ===");
                                        sb.AppendLine($"Source Rect: ({vih.SrcRect.left}, {vih.SrcRect.top}, {vih.SrcRect.right}, {vih.SrcRect.bottom})");
                                        sb.AppendLine($"Target Rect: ({vih.TargetRect.left}, {vih.TargetRect.top}, {vih.TargetRect.right}, {vih.TargetRect.bottom})");
                                        sb.AppendLine($"Bit Rate: {vih.BitRate}");
                                        sb.AppendLine($"Bit Error Rate: {vih.BitErrorRate}");
                                        sb.AppendLine($"Avg Time Per Frame: {vih.AvgTimePerFrame} (100-nanosecond units)");
                                        if (vih.AvgTimePerFrame > 0)
                                        {
                                            var fps = 10000000.0 / vih.AvgTimePerFrame;
                                            sb.AppendLine($"Calculated FPS: {fps:F2}");
                                        }
                                    }
                                    else if (mediaType.formatType == FormatType.VideoInfo2)
                                    {
                                        var vih2 = (VideoInfoHeader2)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader2));
                                        sb.AppendLine("=== VIDEO INFO HEADER 2 ===");
                                        sb.AppendLine($"Source Rect: ({vih2.SrcRect.left}, {vih2.SrcRect.top}, {vih2.SrcRect.right}, {vih2.SrcRect.bottom})");
                                        sb.AppendLine($"Target Rect: ({vih2.TargetRect.left}, {vih2.TargetRect.top}, {vih2.TargetRect.right}, {vih2.TargetRect.bottom})");
                                        sb.AppendLine($"Bit Rate: {vih2.BitRate}");
                                        sb.AppendLine($"Bit Error Rate: {vih2.BitErrorRate}");
                                        sb.AppendLine($"Avg Time Per Frame: {vih2.AvgTimePerFrame} (100-nanosecond units)");
                                        if (vih2.AvgTimePerFrame > 0)
                                        {
                                            var fps = 10000000.0 / vih2.AvgTimePerFrame;
                                            sb.AppendLine($"Calculated FPS: {fps:F2}");
                                        }
                                        sb.AppendLine($"Interlace Flags: 0x{vih2.InterlaceFlags:X8}");
                                        sb.AppendLine($"Copy Protected: {vih2.CopyProtectFlags}");
                                        sb.AppendLine($"Picture Aspect Ratio X: {vih2.PictAspectRatioX}");
                                        sb.AppendLine($"Picture Aspect Ratio Y: {vih2.PictAspectRatioY}");
                                        sb.AppendLine($"Control Flags: 0x{vih2.ControlFlags:X8}");
                                    }
                                }
                            }
                            finally
                            {
                                DsUtils.FreeAMMediaType(mediaType);
                            }
                        }
                        else
                        {
                            sb.AppendLine($"Failed to get media type (HR: 0x{hr:X8})");
                        }
                        
                        sb.AppendLine();
                        sb.AppendLine("=== BUFFER TEST ===");
                        var bufferSize = 0;
                        hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                        if (hr >= 0)
                        {
                            sb.AppendLine($"Current Buffer Size: {bufferSize} bytes");
                            sb.AppendLine($"Allocated Buffer Size: {_frameBuffer?.Length ?? 0} bytes");
                            sb.AppendLine($"Buffer Match: {bufferSize <= (_frameBuffer?.Length ?? 0)}");
                        }
                        else
                        {
                            sb.AppendLine($"Failed to get buffer size (HR: 0x{hr:X8})");
                        }
                    }
                    else
                    {
                        sb.AppendLine("=== SAMPLE GRABBER INFO ===");
                        sb.AppendLine("Sample grabber not available (camera not running)");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== ERROR READING PROPERTIES ===");
                    sb.AppendLine($"Exception: {ex.Message}");
                    sb.AppendLine($"Stack Trace: {ex.StackTrace}");
                }
                
                return sb.ToString();
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
                majorType = MediaType.Video
            };

            var hr = _sampleGrabber.SetMediaType(mediaType);
            DsUtils.FreeAMMediaType(mediaType);
            
            if (hr < 0)
            {
                hr = _sampleGrabber.SetMediaType(null);
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
                if (mediaType.formatType == FormatType.VideoInfo && mediaType.formatPtr != IntPtr.Zero)
                {
                    _videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                    _videoSize = new Size(_videoInfoHeader.BmiHeader.Width, Math.Abs(_videoInfoHeader.BmiHeader.Height));
                    
                    _bitsPerPixel = _videoInfoHeader.BmiHeader.BitCount;
                    _compression = _videoInfoHeader.BmiHeader.Compression;
                    _stride = (_videoSize.Width * _bitsPerPixel + 7) / 8;
                    _stride = (_stride + 3) & ~3;
                    
                    // For compressed formats, allocate larger buffer for compressed data
                    if (_compression != 0)
                        _frameBuffer = new byte[_videoInfoHeader.BmiHeader.ImageSize > 0 ? _videoInfoHeader.BmiHeader.ImageSize : _stride * _videoSize.Height * 2];
                    else
                        _frameBuffer = new byte[_stride * _videoSize.Height];
                }
                else if (mediaType.formatType == FormatType.VideoInfo2 && mediaType.formatPtr != IntPtr.Zero)
                {
                    var videoInfoHeader2 = (VideoInfoHeader2)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader2));
                    _videoSize = new Size(videoInfoHeader2.BmiHeader.Width, Math.Abs(videoInfoHeader2.BmiHeader.Height));
                    
                    _bitsPerPixel = videoInfoHeader2.BmiHeader.BitCount;
                    _compression = videoInfoHeader2.BmiHeader.Compression;
                    _stride = (_videoSize.Width * _bitsPerPixel + 7) / 8;
                    _stride = (_stride + 3) & ~3;
                    
                    // For compressed formats, allocate larger buffer for compressed data
                    if (_compression != 0)
                        _frameBuffer = new byte[videoInfoHeader2.BmiHeader.ImageSize > 0 ? videoInfoHeader2.BmiHeader.ImageSize : _stride * _videoSize.Height * 2];
                    else
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

                // Allocate conversion buffer for RGB24 → RGB32 conversion or MJPEG decompression
                if (_bitsPerPixel == 24 || _compression != 0)
                    _convertedBuffer = new byte[_videoSize.Width * _videoSize.Height * 4];
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
                    _currentOwner = _previewWindow;
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

        public void SetPreviewVisibility(bool isVisible)
        {
            lock (_lock)
            {
                if (_videoWindow == null)
                    return;

                try
                {
                    _videoWindow.put_Visible(isVisible ? OABool.True : OABool.False);
                }
                catch { }
            }
        }

        public void UpdatePreviewWindow(IntPtr ownerHandle, Rectangle location)
        {
            lock (_lock)
            {
                if (_videoWindow != null)
                {
                    try
                    {
                        if (ownerHandle != IntPtr.Zero && _currentOwner != ownerHandle)
                        {
                            // Switch owner without rebuilding the graph
                            _videoWindow.put_Visible(OABool.False);
                            _videoWindow.put_Owner(ownerHandle);
                            _currentOwner = ownerHandle;
                            _videoWindow.put_Visible(OABool.True);
                        }

                        _videoWindow.SetWindowPosition(location.X, location.Y, location.Width, location.Height);
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }
        }

        #endregion

        #region Frame Capture

        /// <summary>
        /// Converts RGB24 (3 bytes per pixel) to RGB32 (4 bytes per pixel with alpha).
        /// Preserves RGB channel order and adds full opacity alpha channel.
        /// </summary>
        static void ConvertRgb24ToRgb32(byte[] src, byte[] dst, int width, int height, int srcStride)
        {
            for (var y = 0; y < height; y++)
            {
                var srcIdx = y * srcStride;
                var dstIdx = y * width * 4;
                
                for (var x = 0; x < width; x++)
                {
                    dst[dstIdx] = src[srcIdx];         // R
                    dst[dstIdx + 1] = src[srcIdx + 1]; // G
                    dst[dstIdx + 2] = src[srcIdx + 2]; // B
                    dst[dstIdx + 3] = 255;             // A (fully opaque)
                    
                    srcIdx += 3;
                    dstIdx += 4;
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

                        // Check if MJPEG compression is used
                        if (_compression != 0)
                        {
                            // MJPEG or other compressed format - decode using System.Drawing
                            try
                            {
                                using (var ms = new System.IO.MemoryStream(_frameBuffer, 0, bufferSize))
                                using (var bitmap = new System.Drawing.Bitmap(ms))
                                {
                                    // Convert bitmap to BGR32 format
                                    var bmpData = bitmap.LockBits(
                                        new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                    
                                    try
                                    {
                                        var sourceStride = bmpData.Stride;
                                        var sourcePtr = bmpData.Scan0;
                                        var targetStride = _videoSize.Width * 4;
                                        
                                        // Copy bitmap data as-is (top-down)
                                        for (int y = 0; y < _videoSize.Height; y++)
                                        {
                                            Marshal.Copy(
                                                sourcePtr + y * sourceStride,
                                                _convertedBuffer,
                                                y * targetStride,
                                                targetStride);
                                        }
                                        
                                        var convertedHandle = GCHandle.Alloc(_convertedBuffer, GCHandleType.Pinned);
                                        try
                                        {
                                            // Use data as-is without flipping
                                            var dataPtr = convertedHandle.AddrOfPinnedObject();
                                            return BitmapLoader.CreateBitmapBgr32(_videoSize, dataPtr, targetStride);
                                        }
                                        finally
                                        {
                                            convertedHandle.Free();
                                        }
                                    }
                                    finally
                                    {
                                        bitmap.UnlockBits(bmpData);
                                    }
                                }
                            }
                            catch
                            {
                                // MJPEG decoding failed, return null
                                return null;
                            }
                        }
                        else if (_bitsPerPixel == 24)
                        {
                            // Convert RGB24 to RGB32 for CreateBitmapBgr32 compatibility
                            ConvertRgb24ToRgb32(_frameBuffer, _convertedBuffer, _videoSize.Width, _videoSize.Height, _stride);
                            
                            var convertedHandle = GCHandle.Alloc(_convertedBuffer, GCHandleType.Pinned);
                            try
                            {
                                var convertedStride = _videoSize.Width * 4;
                                var dataPtr = convertedHandle.AddrOfPinnedObject() + (_videoSize.Height - 1) * convertedStride;
                                return BitmapLoader.CreateBitmapBgr32(_videoSize, dataPtr, -convertedStride);
                            }
                            finally
                            {
                                convertedHandle.Free();
                            }
                        }
                        else
                        {
                            // Pass through other formats (16/32 BPP) as-is
                            var dataPtr = ptr + (_videoSize.Height - 1) * _stride;
                            return BitmapLoader.CreateBitmapBgr32(_videoSize, dataPtr, -_stride);
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
