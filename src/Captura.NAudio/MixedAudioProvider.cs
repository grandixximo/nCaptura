using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Captura.Audio
{
    class MixedAudioProvider : IAudioProvider
    {
        class ProviderState
        {
            public BufferedWaveProvider Buffered { get; set; }
            public ISampleProvider Sample { get; set; }
            public int BytesPerSecond { get; set; }
        }

        readonly Dictionary<NAudioProvider, ProviderState> _providers = new Dictionary<NAudioProvider, ProviderState>();

        readonly IWaveProvider _mixingWaveProvider;
        byte[] _drainBuffer;

        public MixedAudioProvider(params NAudioProvider[] AudioProviders)
        {
            // Choose a sensible target sample rate automatically (OBS defaults to 48 kHz)
            var candidateRates = AudioProviders
                .Select(p => p.NAudioWaveFormat.SampleRate)
                .Where(r => r > 0)
                .ToList();

            var targetRate = candidateRates
                                .GroupBy(r => r)
                                .OrderByDescending(g => g.Count())
                                .ThenByDescending(g => g.Key)
                                .FirstOrDefault()?.Key
                             ?? 48000;

            if (targetRate < 44100)
                targetRate = 44100;

            WaveFormat = new WaveFormat(targetRate, 16, 2);

            foreach (var provider in AudioProviders)
            {
                var bufferedProvider = new BufferedWaveProvider(provider.NAudioWaveFormat)
                {
                    DiscardOnBufferOverflow = true,
                    // Ensure we always get exactly the requested bytes; fills with silence on underflow
                    ReadFully = true
                };

                // Provide headroom against jitter and scheduling delays
                bufferedProvider.BufferDuration = TimeSpan.FromMilliseconds(500);

                provider.WaveIn.DataAvailable += (S, E) =>
                {
                    bufferedProvider.AddSamples(E.Buffer, 0, E.BytesRecorded);
                };

                var sampleProvider = bufferedProvider.ToSampleProvider();

                var providerWf = provider.WaveFormat;

                // Mono to Stereo
                if (providerWf.Channels == 1)
                    sampleProvider = sampleProvider.ToStereo();

                // Resample to the chosen target rate
                if (providerWf.SampleRate != WaveFormat.SampleRate)
                {
                    sampleProvider = new WdlResamplingSampleProvider(sampleProvider, WaveFormat.SampleRate);
                }

                _providers.Add(provider, new ProviderState
                {
                    Buffered = bufferedProvider,
                    Sample = sampleProvider,
                    BytesPerSecond = provider.NAudioWaveFormat.AverageBytesPerSecond
                });
            }

            if (_providers.Count == 1)
            {
                _mixingWaveProvider = _providers
                    .Values
                    .First()
                    .Sample
                    .ToWaveProvider16();
            }
            else
            {
                var waveProviders = _providers.Values.Select(M => M.Sample.ToWaveProvider());

                // MixingSampleProvider cannot be used here due to it removing inputs that don't return as many bytes as requested.

                // Screna expects 44.1 kHz 16-bit Stereo
                _mixingWaveProvider = new MixingWaveProvider32(waveProviders)
                    .ToSampleProvider()
                    .ToWaveProvider16();
            }
        }

        public void Dispose()
        {
            foreach (var provider in _audioProviders.Keys)
            {
                provider.Dispose();
            }
        }

        public WaveFormat WaveFormat { get; }

        public void Start()
        {
            foreach (var provider in _providers.Keys)
            {
                provider.Start();
            }
        }

        public void Stop()
        {
            foreach (var provider in _providers.Keys)
            {
                provider.Stop();
            }
        }

        public int Read(byte[] Buffer, int Offset, int Length)
        {
            BalanceBuffers();
            return _mixingWaveProvider.Read(Buffer, Offset, Length);
        }

        void BalanceBuffers()
        {
            foreach (var state in _providers.Values)
            {
                var bytesPerSecond = state.BytesPerSecond;
                if (bytesPerSecond <= 0)
                    continue;

                var buffered = state.Buffered.BufferedBytes;
                var highWatermark = Math.Min(state.Buffered.BufferLength, (int)(bytesPerSecond * 0.6)); // ~600 ms
                var target = Math.Min(state.Buffered.BufferLength, bytesPerSecond / 2); // ~500 ms

                if (buffered > highWatermark)
                {
                    var toDrop = buffered - target;
                    var maxDrop = Math.Max(bytesPerSecond / 10, 1024); // cap to ~100 ms
                    DropOldest(state.Buffered, Math.Min(toDrop, maxDrop));
                }
            }
        }

        void DropOldest(BufferedWaveProvider provider, int bytes)
        {
            if (bytes <= 0)
                return;

            var chunk = Math.Min(bytes, 16384);
            if (_drainBuffer == null || _drainBuffer.Length < chunk)
                _drainBuffer = new byte[chunk];

            var remaining = bytes;
            while (remaining > 0)
            {
                var toRead = Math.Min(remaining, _drainBuffer.Length);
                var read = provider.Read(_drainBuffer, 0, toRead);
                if (read <= 0)
                    break;
                remaining -= read;
            }
        }
    }
}