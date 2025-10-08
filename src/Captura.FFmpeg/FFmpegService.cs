using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;

namespace Captura.FFmpeg
{
    public static class FFmpegService
    {
        const string FFmpegExeName = "ffmpeg.exe";

        // Track started FFmpeg processes to ensure cleanup on app exit.
        static readonly List<Process> _processes = new List<Process>();
        static readonly object _processesLock = new object();

        static FFmpegService()
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) => KillAll();
            }
            catch { /* Best-effort; ignore environment limitations */ }
        }

        static FFmpegSettings GetSettings() => ServiceProvider.Get<FFmpegSettings>();

        public static bool FFmpegExists
        {
            get
            {
                var folderPath = GetSettings().GetFolderPath();

                // FFmpeg folder
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    var path = Path.Combine(folderPath, FFmpegExeName);

                    if (File.Exists(path))
                        return true;
                }

                if (ServiceProvider.FileExists(FFmpegExeName))
                    return true;

                // PATH
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = FFmpegExeName,
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    return true;
                }
                catch { return false; }
            }
        }

        public static string FFmpegExePath
        {
            get
            {
                var folderPath = GetSettings().GetFolderPath();

                // FFmpeg folder
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    var path = Path.Combine(folderPath, FFmpegExeName);

                    if (File.Exists(path))
                        return path;
                }

                return new[] { ServiceProvider.AppDir, ServiceProvider.LibDir }
                           .Where(M => M != null)
                           .FirstOrDefault(M => File.Exists(Path.Combine(M, FFmpegExeName)))
                       ?? FFmpegExeName;
            }
        }

        public static Process StartFFmpeg(string Arguments, string FileName, out IFFmpegLogEntry FFmpegLog)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = FFmpegExePath,
                    Arguments = Arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                },
                EnableRaisingEvents = true
            };
            
            var log = ServiceProvider.Get<IFFmpegLogRepository>();

            var logItem = log.CreateNew(Path.GetFileName(FileName), Arguments);
            FFmpegLog = logItem;
                        
            process.ErrorDataReceived += (S, E) => logItem.Write(E.Data);

            process.Start();

            process.BeginErrorReadLine();
            
            Register(process);
            
            return process;
        }

        public static bool WaitForConnection(this NamedPipeServerStream ServerStream, int Timeout)
        {
            var asyncResult = ServerStream.BeginWaitForConnection(Ar => {}, null);

            if (asyncResult.AsyncWaitHandle.WaitOne(Timeout))
            {
                ServerStream.EndWaitForConnection(asyncResult);

                return ServerStream.IsConnected;
            }

            return false;
        }

        static void Register(Process process)
        {
            lock (_processesLock)
            {
                _processes.Add(process);
            }

            process.Exited += (s, e) =>
            {
                lock (_processesLock)
                {
                    _processes.Remove(process);
                }
            };
        }

        public static void TryGracefulStop(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    try
                    {
                        // Ask FFmpeg to quit gracefully.
                        process.StandardInput.WriteLine("q");
                        process.StandardInput.Flush();
                        process.StandardInput.Close();
                    }
                    catch { /* Ignore if stdin not available */ }
                }
            }
            catch { }
        }

        public static void KillAll()
        {
            lock (_processesLock)
            {
                foreach (var process in _processes.ToArray())
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                    }
                    catch { }
                }

                _processes.Clear();
            }
        }
    }
}
