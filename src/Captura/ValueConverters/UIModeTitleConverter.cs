using System;
using System.Globalization;
using System.Windows.Data;

namespace Captura
{
    public class UIModeTitleConverter : IValueConverter
    {
        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value is bool useClassicUI && useClassicUI)
            {
                // Classic UI shows timer in title
                try
                {
                    var timerModel = ServiceProvider.Get<TimerModel>();
                    if (timerModel != null && timerModel.TimeSpan != TimeSpan.Zero)
                        return $"Captura - {timerModel.TimeSpan:hh\\:mm\\:ss}";
                }
                catch
                {
                    // If TimerModel not available, just return "Captura"
                }
            }
            
            return "Captura";
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }
}