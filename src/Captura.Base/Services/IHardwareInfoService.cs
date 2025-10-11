namespace Captura
{
    public enum GpuVendor
    {
        Unknown,
        AMD,
        NVIDIA,
        Intel,
        Other
    }

    public interface IHardwareInfoService
    {
        /// <summary>
        /// Gets the primary GPU vendor
        /// </summary>
        GpuVendor GpuVendor { get; }

        /// <summary>
        /// Gets the GPU name/description
        /// </summary>
        string GpuName { get; }

        /// <summary>
        /// Checks if AMD hardware encoding is available
        /// </summary>
        bool HasAmdEncoder { get; }

        /// <summary>
        /// Checks if NVIDIA hardware encoding is available
        /// </summary>
        bool HasNvidiaEncoder { get; }

        /// <summary>
        /// Checks if Intel QuickSync encoding is available
        /// </summary>
        bool HasIntelQuickSync { get; }
    }
}
