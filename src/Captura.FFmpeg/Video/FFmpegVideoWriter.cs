using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Captura.Video;

namespace Captura.FFmpeg
{
    /// <summary>
    /// Encode Video using FFmpeg.exe
    /// </summary>
    class FFmpegWriter : IVideoFileWriter
    {
        readonly NamedPipeServerStream _audioPipe;
        Task _audioConnectTask;

        readonly Process _ffmpegProcess;
        readonly NamedPipeServerStream _ffmpegIn;
        Task _videoConnectTask;
        byte[] _videoBuffer;

        readonly ConcurrentQueue<byte[]> _bufferPool = new ConcurrentQueue<byte[]>();

        static string GetPipeName() => $"captura-{Guid.NewGuid()}";

        /// <summary>
        /// Creates a new instance of <see cref="FFmpegWriter"/>.
        /// </summary>
        public FFmpegWriter(FFmpegVideoWriterArgs Args)
        {
            if (!FFmpegService.FFmpegExists)
            {
                throw new FFmpegNotFoundException();
            }

            var nv12 = Args.ImageProvider.DummyFrame is INV12Frame;

            var settings = ServiceProvider.Get<FFmpegSettings>();

            var w = Args.ImageProvider.Width;
            var h = Args.ImageProvider.Height;

            _videoBuffer = new byte[(int)(w * h * (nv12 ? 1.5 : 4))];

            Console.WriteLine($"Video Buffer Allocated: {_videoBuffer.Length}");

            var videoPipeName = GetPipeName();

            var argsBuilder = new FFmpegArgsBuilder();

            argsBuilder.AddInputPipe(videoPipeName)
                .AddArg("thread_queue_size", 512)
                .AddArg("framerate", Args.FrameRate)
                .SetFormat("rawvideo")
                .AddArg("pix_fmt", nv12 ? "nv12" : "rgb32")
                .SetVideoSize(w, h);

            var output = argsBuilder.AddOutputFile(Args.FileName)
                .SetFrameRate(Args.FrameRate);

            Args.VideoCodec.Apply(settings, Args, output);
            
            if (settings.Resize)
            {
                var width = settings.ResizeWidth;
                var height = settings.ResizeHeight;

                if (width % 2 == 1)
                    ++width;

                if (height % 2 == 1)
                    ++height;

                output.AddArg("vf", $"scale={width}:{height}");
            }

            if (Args.AudioProvider != null)
            {
                var audioPipeName = GetPipeName();

                argsBuilder.AddInputPipe(audioPipeName)
                    .AddArg("thread_queue_size", 512)
                    .SetFormat("s16le")
                    .SetAudioCodec("pcm_s16le")
                    .SetAudioFrequency(Args.Frequency)
                    .SetAudioChannels(Args.Channels);

                Args.VideoCodec.AudioArgsProvider(Args.AudioQuality, output);

                var wf = Args.AudioProvider.WaveFormat;

                _audioBytesPerFrame = (int)((1.0 / Args.FrameRate)
                                            * wf.SampleRate
                                            * wf.Channels
                                            * (wf.BitsPerSample / 8.0));

                // Modest buffer size to avoid stalls without huge allocation
                var audioBufferSize = 16384;

                _audioPipe = new NamedPipeServerStream(audioPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, audioBufferSize, audioBufferSize);
            }

            _ffmpegIn = new NamedPipeServerStream(videoPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, _videoBuffer.Length, _videoBuffer.Length);

            // Put both pipes into listening state BEFORE launching FFmpeg
            _videoConnectTask = BeginListening(_ffmpegIn);
            if (_audioPipe != null) _audioConnectTask = BeginListening(_audioPipe);

            _ffmpegProcess = FFmpegService.StartFFmpeg(argsBuilder.GetArgs(), Args.FileName, out _);
        }

        static Task BeginListening(NamedPipeServerStream pipe)
        {
            try
            {
                var tcs = new TaskCompletionSource<object>();
                pipe.BeginWaitForConnection(ar =>
                {
                    try { pipe.EndWaitForConnection(ar); tcs.TrySetResult(null); }
                    catch (Exception e) { tcs.TrySetException(e); }
                }, null);
                return tcs.Task;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        public void Dispose()
        {
            try
            {
                try { _lastFrameTask?.Wait(5000); } catch { }
                try { _lastAudio?.Wait(5000); } catch { }

                try
                {
                    _ffmpegIn?.Flush();
                    _audioPipe?.Flush();
                }
                catch { }

                try { _ffmpegIn?.Dispose(); } catch { }
                try { _audioPipe?.Dispose(); } catch { }

                FFmpegService.TryGracefulStop(_ffmpegProcess);

                if (!_ffmpegProcess.WaitForExit(10000))
                {
                    try
                    {
                        _ffmpegProcess.Kill();
                        _ffmpegProcess.WaitForExit(2000);
                    }
                    catch { }
                }
            }
            finally
            {
                _ffmpegProcess?.Dispose();
                _videoBuffer = null;
            }
        }

        /// <summary>
        /// Gets whether audio is supported.
        /// </summary>
        public bool SupportsAudio { get; } = true;

        bool _firstAudio = true;

        Task _lastAudio;

        /// <summary>
        /// Write audio block to Audio Stream.
        /// </summary>
        /// <param name="Buffer">Buffer containing audio data.</param>
        /// <param name="Length">Length of audio data in bytes.</param>
        public void WriteAudio(byte[] Buffer, int Offset, int Length)
        {
            // Might happen when writing Gif
            if (_audioPipe == null)
                return;

            if (_ffmpegProcess.HasExited)
            {
                throw new FFmpegException( _ffmpegProcess.ExitCode);
            }

            if (_firstAudio)
            {
                if (_audioConnectTask != null)
                {
                    if (!_audioConnectTask.Wait(5000))
                        throw new Exception("Cannot connect Audio pipe to FFmpeg");
                }
                _firstAudio = false;
            }

            if (_lastAudio != null)
            {
                try { _lastAudio.Wait(1000); } catch { }
            }


            _lastAudio = _audioPipe.WriteAsync(Buffer, Offset, Length);
        }

        bool _firstFrame = true;
        readonly int _audioBytesPerFrame;
        Task _lastFrameTask;

        /// <summary>
        /// Writes an Image frame.
        /// </summary>
        public void WriteFrame(IBitmapFrame Frame)
        {
            if (_ffmpegProcess.HasExited)
            {
                Frame.Dispose();
                throw new FFmpegException(_ffmpegProcess.ExitCode);
            }
            
            if (_firstFrame)
            {
                if (_videoConnectTask != null)
                {
                    if (!_videoConnectTask.Wait(5000))
                        throw new Exception("Cannot connect Video pipe to FFmpeg");
                }
                _firstFrame = false;
            }

            if (_lastFrameTask == null)
            {
                _lastFrameTask = Task.CompletedTask;
            }

            if (!(Frame is RepeatFrame))
            {
                using (Frame)
                {
                    if (Frame.Unwrap() is INV12Frame nv12Frame)
                    {
                        nv12Frame.CopyNV12To(_videoBuffer);
                    }
                    else Frame.CopyTo(_videoBuffer);
                }
            }

            var bufferCopy = _bufferPool.TryDequeue(out var pooledBuffer) ? pooledBuffer : new byte[_videoBuffer.Length];
            Buffer.BlockCopy(_videoBuffer, 0, bufferCopy, 0, _videoBuffer.Length);

            _lastFrameTask = (_lastFrameTask ?? Task.CompletedTask).ContinueWith(async previousTask =>
            {
                try
                {
                    await previousTask;
                    await _ffmpegIn.WriteAsync(bufferCopy, 0, bufferCopy.Length);
                }
                catch (Exception ex) when (!(_ffmpegProcess?.HasExited == false))
                {
                    throw new FFmpegException(_ffmpegProcess?.ExitCode ?? -1, ex);
                }
                finally
                {
                    _bufferPool.Enqueue(bufferCopy);
                }
            }, TaskContinuationOptions.RunContinuationsAsynchronously).Unwrap();
        }
    }
}
