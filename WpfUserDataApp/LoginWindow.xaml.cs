using System;
using System.Windows;
using UserDataLibrary.Exceptions;
using UserDataLibrary.Services;
using WpfUserDataApp.Utils;

namespace WpfUserDataApp
{
    public partial class LoginWindow : Window
    {
        private readonly UserService _userService;

        public LoginWindow()
        {
            InitializeComponent();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Инициализация _userService остается здесь
            _userService = new UserService(baseDir);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // ... (код LoginButton_Click остается прежним) ...
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
            catch (AuthenticationException authEx)
            {
                ErrorTextBlock.Text = authEx.Message;
                ErrorLogger.LogError(authEx, $"Login attempt failed for user: {username}");
            }
            catch (DataAccessException dataEx)
            {
                ErrorTextBlock.Text = "Ошибка доступа к данным пользователей.";
                ErrorLogger.LogError(dataEx, "Login failed due to data access issue");
                MessageBox.Show($"Критическая ошибка доступа к данным:\n{dataEx.Message}\n\nПриложение будет закрыто.",
                                "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = "Произошла непредвиденная ошибка.";
                ErrorLogger.LogError(ex, "Unexpected error during login");
                MessageBox.Show($"Произошла непредвиденная ошибка:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // НОВЫЙ ОБРАБОТЧИК для кнопки Регистрация
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            // Показываем окно регистрации как модальное
            // Можно проверить результат (registrationWindow.ShowDialog()), если нужно
            // что-то сделать после успешной регистрации, но обычно это не требуется.
            registrationWindow.ShowDialog();
        }
    }
}