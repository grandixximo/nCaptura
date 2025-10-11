using System;
using System.Threading.Tasks;

namespace Captura.Audio
{
    public interface IAudioItem : IDisposable
    {
        string Name { get; }

        bool IsLoopback { get; }

        Task StartListeningForPeakLevelAsync();

        void StopListeningForPeakLevel();

        double PeakLevel { get; }
    }
}
