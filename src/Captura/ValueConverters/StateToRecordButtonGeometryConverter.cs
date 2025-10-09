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
                    System.Diagnostics.Debug.WriteLine("[StateToRecordButtonGeometryConverter] IIconSet is null!");
                    return Binding.DoNothing;
                }

                if (Value is RecorderState state)
                {
                    var iconString = state == RecorderState.NotRecording
                        ? icons.Record
                        : icons.Stop;
                    
                    System.Diagnostics.Debug.WriteLine($"[StateToRecordButtonGeometryConverter] State: {state}, Icon: {iconString?.Substring(0, Math.Min(30, iconString?.Length ?? 0))}...");
                    
                    if (string.IsNullOrEmpty(iconString))
                    {
                        System.Diagnostics.Debug.WriteLine("[StateToRecordButtonGeometryConverter] Icon string is null or empty!");
                        return Binding.DoNothing;
                    }
                    
                    return Geometry.Parse(iconString);
                }
                
                System.Diagnostics.Debug.WriteLine($"[StateToRecordButtonGeometryConverter] Value is not RecorderState: {Value?.GetType().Name ?? "null"}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StateToRecordButtonGeometryConverter] Exception: {ex.Message}");
            }

            return Binding.DoNothing;
        }
    }
}