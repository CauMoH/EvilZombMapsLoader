using EvilZombMapsLoader.Extensions.Attributes;
using System;
using System.Globalization;
using System.Windows.Data;

namespace EvilZombMapsLoader.Converters
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            var result = Description.GetDescription((Enum)value);
            return result;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
