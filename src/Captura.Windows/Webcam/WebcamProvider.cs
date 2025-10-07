using System.Collections.Generic;
using System.Linq;

namespace Captura.Webcam
{
    /// <summary>
    /// Provides access to available webcam devices
    /// </summary>
    class WebcamProvider : NotifyPropertyChanged, IWebCamProvider
    {
        public IEnumerable<IWebcamItem> GetSources()
        {
            try
            {
                return Filter.VideoInputDevices
                    .Where(device => device != null && !string.IsNullOrEmpty(device.Name))
                    .Select(device => new WebcamItem(device))
                    .ToList();
            }
            catch
            {
                // Return empty list on error rather than crashing
                return new List<IWebcamItem>();
            }
        }
    }
}
