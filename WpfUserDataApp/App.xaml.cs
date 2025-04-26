using System;
using System.Windows;
using WpfUserDataApp.Utils; // Логгер

namespace WpfUserDataApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Настройка глобального обработчика необработанных исключений
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            // Создаем и показываем окно входа
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // MainWindow больше не создается здесь автоматически
        }

        // Обработчик необработанных исключений в потоке UI
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorLogger.LogError(e.Exception, "Unhandled UI Exception");
            MessageBox.Show($"Произошла необработанная ошибка UI:\n{e.Exception.Message}\n\nПриложение может работать нестабильно. Рекомендуется перезапуск.",
                            "Необработанная ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Помечаем как обработанное, чтобы приложение не падало сразу (можно изменить на false, если нужно падение)
        }

        // Обработчик необработанных исключений в других потоках
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                ErrorLogger.LogError(ex, $"Unhandled Non-UI Exception (IsTerminating: {e.IsTerminating})");
                // Попытка показать сообщение, но может не сработать, если UI поток недоступен
                try
                {
                    MessageBox.Show($"Произошла критическая необработанная ошибка:\n{ex.Message}\n\nПриложение будет закрыто.",
                                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { /* Игнорируем ошибки показа MessageBox здесь */ }
            }
            else
            {
                ErrorLogger.LogError(new Exception($"Unknown non-UI exception object: {e.ExceptionObject}"), "Unhandled Non-UI Exception (Unknown Type)");
            }
            // Приложение, скорее всего, завершится после этого, особенно если e.IsTerminating == true
        }
    }
}