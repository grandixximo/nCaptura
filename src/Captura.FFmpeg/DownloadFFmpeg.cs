using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Captura.FFmpeg
{
    public static class DownloadFFmpeg
    {
        static readonly Uri[] FFmpegMirrors;
        static readonly string FFmpegArchivePath;

        static DownloadFFmpeg()
        {
            var arch = Environment.Is64BitOperatingSystem ? "win64" : "win32";
            
            // Multiple reliable mirrors with fallback support
            FFmpegMirrors = new[]
            {
                // Primary: gyan.dev (very reliable, updated regularly)
                new Uri($"https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"),
                
                // Fallback 1: GitHub BtbN builds
                new Uri($"https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-{arch}-gpl.zip"),
                
                // Fallback 2: Alternate gyan.dev URL
                new Uri($"https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip")
            };

            FFmpegArchivePath = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");
        }

        public static async Task DownloadArchive(Action<int> Progress, IWebProxy Proxy, CancellationToken CancellationToken)
        {
            Exception lastException = null;
            
            // Try each mirror until one succeeds
            foreach (var mirror in FFmpegMirrors)
            {
                try
                {
                    using var webClient = new WebClient { Proxy = Proxy };
                    CancellationToken.Register(() => webClient.CancelAsync());

                    webClient.DownloadProgressChanged += (S, E) =>
                    {
                        Progress?.Invoke(E.ProgressPercentage);
                    };
                    
                    // Set timeout and user agent
                    webClient.Headers.Add("User-Agent", "nCaptura-FFmpegDownloader/1.0");
                    
                    await webClient.DownloadFileTaskAsync(mirror, FFmpegArchivePath);
                    
                    // If we reach here, download succeeded
                    return;
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
                {
                    // User cancelled, don't try other mirrors
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    // Try next mirror
                    continue;
                }
            }
            
            // All mirrors failed
            throw new Exception($"Failed to download FFmpeg from all mirrors. Last error: {lastException?.Message}", lastException);
        }

        const string ExeName = "ffmpeg.exe";

        public static async Task ExtractTo(string FolderPath)
        {
            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(FFmpegArchivePath);
                
                // Find ffmpeg.exe in the archive (may be in subdirectories)
                var ffmpegEntry = archive.Entries.FirstOrDefault(M => 
                    M.Name.Equals(ExeName, StringComparison.OrdinalIgnoreCase) && 
                    M.FullName.IndexOf("bin", StringComparison.OrdinalIgnoreCase) >= 0);
                
                // If not found in bin folder, search anywhere
                if (ffmpegEntry == null)
                {
                    ffmpegEntry = archive.Entries.FirstOrDefault(M => 
                        M.Name.Equals(ExeName, StringComparison.OrdinalIgnoreCase));
                }
                
                if (ffmpegEntry == null)
                {
                    throw new FileNotFoundException($"Could not find {ExeName} in the downloaded archive");
                }

                var destinationPath = Path.Combine(FolderPath, ExeName);
                ffmpegEntry.ExtractToFile(destinationPath, true);
                
                // Verify the extracted file
                if (!File.Exists(destinationPath))
                {
                    throw new FileNotFoundException($"Failed to extract {ExeName}");
                }
            });
        }
    }
}