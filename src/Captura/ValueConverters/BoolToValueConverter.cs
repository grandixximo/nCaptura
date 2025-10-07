using System;
using System.Globalization;
using System.Windows.Data;

namespace Captura
{
    public class BoolToValueConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            if (Value is bool boolValue)
            {
                return boolValue ? TrueValue : FalseValue;
            }
            return FalseValue;
        }

        public object ConvertBack(object Value, Type TargetType, object Parameter, CultureInfo Culture)
        {
            throw new NotImplementedException();
        }
    }
}