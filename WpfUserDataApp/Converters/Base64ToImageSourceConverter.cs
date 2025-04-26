using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using UserDataLibrary.Helpers; // Наш хелпер

namespace WpfUserDataApp.Converters
{
    public class Base64ToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string base64String && targetType == typeof(ImageSource))
            {
                return ImageHelper.Base64ToImageSource(base64String);
            }
            return null; // Или DependencyProperty.UnsetValue
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Обратное преобразование не требуется для отображения
            throw new NotImplementedException();
        }
    }
}