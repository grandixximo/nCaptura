using Captura.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Captura
{
    public class StateToRecordButtonGeometryConverter : OneWayConverter
    {
        public override object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            try
            {
                var icons = ServiceProvider.Get<IIconSet>();
                
                if (icons == null)
                {
                    // Return a simple circle as fallback
                    return Geometry.Parse("M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z");
                }

                if (Value is RecorderState state)
                {
                    var iconString = state == RecorderState.NotRecording
                        ? icons.Record
                        : icons.Stop;
                    
                    if (string.IsNullOrEmpty(iconString))
                    {
                        // Return a simple square as fallback
                        return Geometry.Parse("M5,5H19V19H5V5Z");
                    }
                    
                    return Geometry.Parse(iconString);
                }
            }
            catch (Exception ex)
            {
                // Return a simple X as fallback to show there was an error
                return Geometry.Parse("M19,6.41L17.59,5L12,10.59L6.41,5L5,6.41L10.59,12L5,17.59L6.41,19L12,13.41L17.59,19L19,17.59L13.41,12L19,6.41Z");
            }

            // Default fallback - simple circle
            return Geometry.Parse("M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z");
        }
    }
}