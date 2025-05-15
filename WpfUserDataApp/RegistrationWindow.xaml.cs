using System;
using System.Windows;
using UserDataLibrary.Exceptions;
using UserDataLibrary.Services;
using WpfUserDataApp.Utils; // Логгер

namespace WpfUserDataApp
{
    public partial class RegistrationWindow : Window
    {
        private readonly UserService _userService;

        public RegistrationWindow()
        {
            InitializeComponent();
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            // Предполагаем, что UserService уже был создан в LoginWindow или App,
            // но для простоты создадим новый экземпляр здесь.
            // В более сложном приложении лучше использовать Dependency Injection
            // или передавать существующий экземпляр.
            try
            {
                _userService = new UserService(baseDir);
            }
            catch (Exception ex) // Ловим ошибки инициализации UserService (например, не удалось создать файл)
            {
                ErrorLogger.LogError(ex, "RegistrationWindow - UserService initialization failed");
                MessageBox.Show($"Критическая ошибка при инициализации сервиса пользователей:\n{ex.Message}\n\nОкно регистрации не может быть открыто.",
                                "Ошибка инициализации", MessageBoxButton.OK, MessageBoxImage.Error);
                // Закрываем окно сразу, если сервис недоступен
                this.Loaded += (s, e) => this.Close(); // Закроет после загрузки
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, был ли успешно инициализирован сервис
            if (_userService == null)
            {
                ErrorTextBlock.Text = "Сервис пользователей недоступен.";
                return;
            }

            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            ErrorTextBlock.Text = ""; // Очищаем предыдущие ошибки

            // --- Валидация на стороне UI ---
            if (string.IsNullOrWhiteSpace(username))
            {
                ErrorTextBlock.Text = "Имя пользователя не может быть пустым.";
                UsernameTextBox.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorTextBlock.Text = "Пароль не может быть пустым.";
                PasswordBox.Focus();
                return;
            }
            if (password != confirmPassword)
            {
                ErrorTextBlock.Text = "Пароли не совпадают.";
                ConfirmPasswordBox.Focus();
                ConfirmPasswordBox.SelectAll();
                return;
            }
            // Проверка на недопустимые символы (если есть)
            if (username.Contains(':'))
            {
                ErrorTextBlock.Text = "Имя пользователя не может содержать символ ':'.";
                UsernameTextBox.Focus();
                return;
            }


            // --- Попытка регистрации через сервис ---
            try
            {
                _userService.AddUser(username, password);

                // Успешная регистрация
                MessageBox.Show($"Пользователь '{username}' успешно зарегистрирован!",
                                "Регистрация завершена", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Устанавливаем результат для вызывающего окна (если нужно)
                this.Close(); // Закрываем окно регистрации
            }
            catch (UserAlreadyExistsException uaex) // Пользователь уже существует
            {
                ErrorTextBlock.Text = uaex.Message;
                ErrorLogger.LogError(uaex, $"Registration attempt failed for existing user: {username}");
                UsernameTextBox.Focus();
                UsernameTextBox.SelectAll();
            }
            catch (DataAccessException daex) // Ошибка доступа к файлу
            {
                ErrorTextBlock.Text = "Ошибка при доступе к файлу пользователей. Регистрация не удалась.";
                ErrorLogger.LogError(daex, $"Registration failed due to data access issue for user: {username}");
                MessageBox.Show($"Ошибка доступа к данным:\n{daex.Message}", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ArgumentException argEx) // Невалидные аргументы (например, ':' в имени)
            {
                ErrorTextBlock.Text = argEx.Message;
                ErrorLogger.LogError(argEx, $"Registration failed due to invalid argument for user: {username}");
            }
            catch (Exception ex) // Другие непредвиденные ошибки
            {
                ErrorTextBlock.Text = "Произошла непредвиденная ошибка при регистрации.";
                ErrorLogger.LogError(ex, $"Unexpected error during registration for user: {username}");
                MessageBox.Show($"Произошла непредвиденная ошибка:\n{ex.Message}", "Ошибка регистрации", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Устанавливаем результат (Отмена)
            this.Close(); // Закрываем окно
        }
    }
}