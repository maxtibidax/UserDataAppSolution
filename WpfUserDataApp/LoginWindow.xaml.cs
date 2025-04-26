using System;
using System.Windows;
using UserDataLibrary.Exceptions;
using UserDataLibrary.Services;
using WpfUserDataApp.Utils; // Для логгера

namespace WpfUserDataApp
{
    public partial class LoginWindow : Window
    {
        private readonly UserService _userService;

        public LoginWindow()
        {
            InitializeComponent();
            // Получаем базовую директорию приложения
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _userService = new UserService(baseDir);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            ErrorTextBlock.Text = ""; // Очищаем предыдущие ошибки

            try
            {
                var authenticatedUser = _userService.Authenticate(username, password);

                // Успешный вход
                var mainWindow = new MainWindow(authenticatedUser.Username);
                mainWindow.Show();
                this.Close(); // Закрываем окно входа
            }
            catch (AuthenticationException authEx) // Ловим UserNotFound, InvalidPassword и другие ошибки аутентификации
            {
                ErrorTextBlock.Text = authEx.Message;
                ErrorLogger.LogError(authEx, $"Login attempt failed for user: {username}");
            }
            catch (DataAccessException dataEx) // Ловим ошибки доступа к файлу пользователей
            {
                ErrorTextBlock.Text = "Ошибка доступа к данным пользователей. Проверьте файл users.txt и права доступа.";
                ErrorLogger.LogError(dataEx, "Login failed due to data access issue");
                MessageBox.Show($"Критическая ошибка доступа к данным:\n{dataEx.Message}\n\nПриложение будет закрыто.",
                                "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(); // Завершаем приложение при невозможности читать файл пользователей
            }
            catch (Exception ex) // Ловим любые другие неожиданные ошибки
            {
                ErrorTextBlock.Text = "Произошла непредвиденная ошибка.";
                ErrorLogger.LogError(ex, "Unexpected error during login");
                MessageBox.Show($"Произошла непредвиденная ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}