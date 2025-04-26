using System;
using System.IO;
using System.Windows.Media; // Добавь ссылку на PresentationCore (для WPF типов)
using System.Windows.Media.Imaging; // Добавь ссылку на PresentationCore

namespace UserDataLibrary.Helpers
{
    public static class ImageHelper
    {
        // Конвертация файла изображения в Base64
        public static string ImageFileToBase64(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                byte[] imageBytes = File.ReadAllBytes(filePath);
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex) // Ловим общие ошибки чтения файла
            {
                // Здесь можно логировать ошибку или пробросить специфическое исключение
                Console.WriteLine($"Error converting image file to Base64: {ex.Message}");
                return null; // Или пробросить исключение
            }
        }

        // Конвертация Base64 строки в ImageSource для WPF
        public static ImageSource Base64ToImageSource(string base64String)
        {
            if (string.IsNullOrEmpty(base64String)) return null;

            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad; // Важно для освобождения потока
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze(); // Делаем объект неизменяемым для потокобезопасности
                    return image;
                }
            }
            catch (FormatException ex) // Ошибка формата Base64
            {
                Console.WriteLine($"Error decoding Base64 string: {ex.Message}");
                return null;
            }
            catch (Exception ex) // Другие возможные ошибки (например, NotSupportedException для формата изображения)
            {
                Console.WriteLine($"Error converting Base64 to ImageSource: {ex.Message}");
                return null;
            }
        }
    }
}