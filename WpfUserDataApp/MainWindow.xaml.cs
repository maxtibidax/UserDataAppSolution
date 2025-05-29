using System;
using System.Linq;
using System.Windows;
using UserDataLibrary.Exceptions; // Импорт пользовательских исключений
using UserDataLibrary.Models;    // Импорт модели StudentData
using UserDataLibrary.Services;   // Импорт сервисов (DataService)
using WpfUserDataApp.Utils;       // Импорт логгера
using System.Text; // For Encoding in File.WriteAllText (if ReportService didn't use it)
using Microsoft.Win32; // For SaveFileDialog
using System.Diagnostics; // For Process.Start

namespace WpfUserDataApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _currentUsername;
        private readonly DataService _dataService;
        private readonly ReportService _reportService;

        public MainWindow(string username)
        {
            InitializeComponent();
            _currentUsername = username;
            CurrentUserTextBlock.Text = _currentUsername; // Отображаем имя пользователя в UI

            string baseDir = AppDomain.CurrentDomain.BaseDirectory; // Путь к папке приложения
            _reportService = new ReportService();

            try
            {
                // Инициализируем сервис данных. Это может вызвать ошибку при чтении файла data.json
                _dataService = new DataService(baseDir);
                // Загружаем данные для текущего пользователя
                LoadUserData();
            }
            catch (DataAccessException daEx) // Ловим ошибки доступа к файлу данных при инициализации
            {
                // Логируем критическую ошибку
                ErrorLogger.LogError(daEx, "MainWindow initialization - DataService creation failed");
                // Показываем сообщение пользователю
                MessageBox.Show($"Критическая ошибка доступа к файлу данных:\n{daEx.Message}\n\nПриложение будет закрыто.",
                                 "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                // Закрываем приложение, так как без DataService оно работать не может
                Application.Current.Shutdown();
            }
            catch (Exception ex) // Ловим другие возможные ошибки при инициализации
            {
                ErrorLogger.LogError(ex, "MainWindow initialization failed unexpectedly");
                MessageBox.Show($"Произошла критическая ошибка при инициализации приложения:\n{ex.Message}\n\nПриложение будет закрыто.",
                                 "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        // Метод для загрузки (или перезагрузки) данных студента в DataGrid
        private void DataItemsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int selectedCount = DataItemsGrid.SelectedItems.Count;

            SingleStudentReportButton.IsEnabled = selectedCount == 1;
            MultipleStudentsReportButton.IsEnabled = selectedCount > 0;
            AggregateReportButton.IsEnabled = selectedCount > 0;
        }
        // Helper method to show SaveFileDialog and get file path
        private string GetReportFilePath(string defaultFileName)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
                FileName = defaultFileName,
                DefaultExt = ".html",
                Title = "Сохранить отчет как..."
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }
            return null;
        }

        // Helper method to open report
        private void TryOpenReport(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(ex, $"Failed to open report file: {filePath}");
                MessageBox.Show($"Не удалось автоматически открыть отчет: {ex.Message}", "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SingleStudentReportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudent = DataItemsGrid.SelectedItem as StudentData;
            if (selectedStudent == null)
            {
                MessageBox.Show("Пожалуйста, выберите одного студента для отчета.", "Выбор студента", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filePath = GetReportFilePath($"Отчет_{selectedStudent.FullName.Replace(" ", "_")}.html");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                _reportService.GenerateSingleStudentReport(selectedStudent, filePath);
                MessageBox.Show($"Отчет успешно создан: {filePath}", "Генерация отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                TryOpenReport(filePath);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(ex, $"Generating single student report for {selectedStudent.FullName}");
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MultipleStudentsReportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudents = DataItemsGrid.SelectedItems.Cast<StudentData>().ToList();
            if (!selectedStudents.Any())
            {
                MessageBox.Show("Пожалуйста, выберите одного или нескольких студентов для отчета.", "Выбор студентов", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filePath = GetReportFilePath("Отчет_выбранные_студенты.html");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                _reportService.GenerateMultipleStudentsReport(selectedStudents, filePath);
                MessageBox.Show($"Отчет успешно создан: {filePath}", "Генерация отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                TryOpenReport(filePath);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(ex, $"Generating multiple students report. Count: {selectedStudents.Count}");
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}", "Ошибка отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AggregateReportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedStudents = DataItemsGrid.SelectedItems.Cast<StudentData>().ToList();
            if (!selectedStudents.Any())
            {
                MessageBox.Show("Пожалуйста, выберите одного или нескольких студентов для сводного отчета.", "Выбор студентов", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string filePath = GetReportFilePath("Сводный_отчет_студенты.html");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                _reportService.GenerateAggregateReport(selectedStudents, filePath);
                MessageBox.Show($"Сводный отчет успешно создан: {filePath}", "Генерация отчета", MessageBoxButton.OK, MessageBoxImage.Information);
                TryOpenReport(filePath);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError(ex, $"Generating aggregate report. Count: {selectedStudents.Count}");
                MessageBox.Show($"Ошибка при создании сводного отчета: {ex.Message}", "Ошибка отчета", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void LoadUserData()
        {
            try
            {
                // Получаем список студентов для текущего пользователя из сервиса
                var userData = _dataService.GetDataForUser(_currentUsername);
                // Устанавливаем этот список как источник данных для DataGrid
                DataItemsGrid.ItemsSource = userData;
            }
            catch (DataAccessException dataEx) // Ошибка при *чтении* данных (файл мог испортиться после запуска)
            {
                MessageBox.Show($"Ошибка загрузки данных студентов: {dataEx.Message}", "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                ErrorLogger.LogError(dataEx, $"Loading student data for user '{_currentUsername}'");
                DataItemsGrid.ItemsSource = null; // Очищаем грид в случае ошибки
            }
            catch (Exception ex) // Другие непредвиденные ошибки при загрузке
            {
                MessageBox.Show($"Произошла непредвиденная ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ErrorLogger.LogError(ex, $"Unexpected error loading student data for user '{_currentUsername}'");
                DataItemsGrid.ItemsSource = null; // Очищаем грид
            }
        }

        // Обработчик нажатия кнопки "Обновить"
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUserData(); // Просто перезагружаем данные
        }

        // Обработчик нажатия кнопки "Добавить"
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем окно добавления/редактирования в режиме добавления
            var addEditWindow = new AddEditDataWindow(_currentUsername);
            // Показываем окно как модальное (блокирует основное окно)
            // и получаем результат (true если нажали "Сохранить", false/null если "Отмена" или закрыли)
            var result = addEditWindow.ShowDialog();

            // Если пользователь нажал "Сохранить" в диалоговом окне
            if (result == true)
            {
                try
                {
                    // Получаем созданный объект студента из окна добавления
                    StudentData newStudent = addEditWindow.CurrentStudentData;
                    // Добавляем студента через сервис данных (он сохранит изменения в файл)
                    _dataService.AddStudentData(newStudent);
                    // Обновляем DataGrid, чтобы показать нового студента
                    LoadUserData();
                }
                catch (DataAccessException dataEx) // Ошибка при сохранении данных
                {
                    MessageBox.Show($"Ошибка сохранения данных студента: {dataEx.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(dataEx, $"Adding student data for user '{_currentUsername}'");
                }
                catch (Exception ex) // Другие ошибки при добавлении
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при добавлении студента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ErrorLogger.LogError(ex, $"Unexpected error adding student data for user '{_currentUsername}'");
                }
            }
        }

        // Обработчик нажатия кнопки "Редактировать"
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранного студента из DataGrid
            var selectedStudent = DataItemsGrid.SelectedItem as StudentData;

            // Проверяем, выбран ли кто-то
            if (selectedStudent == null)
            {
                MessageBox.Show("Пожалуйста, выберите студента для редактирования.", "Редактирование", MessageBoxButton.OK, MessageBoxImage.Information);
                return; // Выходим из метода, если ничего не выбрано
            }

            // Создаем КОПИЮ выбранного объекта для редактирования.
            // Это важно, чтобы изменения в окне редактирования не отражались
            // в DataGrid до нажатия кнопки "Сохранить".
            var studentToEdit = ShallowCopy(selectedStudent);

            // Создаем окно добавления/редактирования в режиме редактирования, передавая копию объекта
            var addEditWindow = new AddEditDataWindow(_currentUsername, studentToEdit);
            var result = addEditWindow.ShowDialog(); // Показываем окно

            // Если пользователь нажал "Сохранить"
            if (result == true)
            {
                try
                {
                    // Получаем измененный объект из окна редактирования
                    StudentData updatedStudent = addEditWindow.CurrentStudentData;
                    // Обновляем данные студента через сервис (он сохранит изменения в файл)
                    _dataService.UpdateStudentData(updatedStudent);
                    // Обновляем DataGrid
                    LoadUserData();
                }
                catch (DataObjectNotFoundException notFoundEx) // Студент не найден (мог быть удален)
                {
                    MessageBox.Show(notFoundEx.Message, "Ошибка редактирования", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(notFoundEx, $"Editing student data (ID: {selectedStudent.Id}) for user '{_currentUsername}' - Not Found");
                    LoadUserData(); // Обновляем грид на случай, если объект удалили
                }
                catch (DataAccessException dataEx) // Ошибка при сохранении
                {
                    MessageBox.Show($"Ошибка сохранения данных студента: {dataEx.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(dataEx, $"Editing student data (ID: {selectedStudent.Id}) for user '{_currentUsername}'");
                }
                catch (InvalidOperationException opEx) // Попытка изменить чужого студента (теоретически)
                {
                    MessageBox.Show(opEx.Message, "Ошибка операции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(opEx, $"Editing student data (ID: {selectedStudent.Id}) - Operation Denied for {_currentUsername}");
                }
                catch (Exception ex) // Другие ошибки
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при редактировании студента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ErrorLogger.LogError(ex, $"Unexpected error editing student data (ID: {selectedStudent.Id}) for user '{_currentUsername}'");
                }
            }
        }

        // Вспомогательный метод для создания поверхностной копии объекта StudentData
        // Используется, чтобы передать в окно редактирования копию, а не сам объект из DataGrid
        private StudentData ShallowCopy(StudentData original)
        {
            // Используем встроенный метод MemberwiseClone, доступный через рефлексию
            // Для StudentData, где все поля - значимые типы или строки, этого достаточно.
            // Если бы были вложенные изменяемые классы, потребовалось бы глубокое копирование.
            return (StudentData)original.GetType().GetMethod("MemberwiseClone",
                       System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                       .Invoke(original, null);
        }


        // Обработчик нажатия кнопки "Удалить"
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранного студента
            var selectedStudent = DataItemsGrid.SelectedItem as StudentData;

            // Проверяем, выбран ли кто-то
            if (selectedStudent == null)
            {
                MessageBox.Show("Пожалуйста, выберите студента для удаления.", "Удаление", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Запрашиваем подтверждение у пользователя
            var confirmResult = MessageBox.Show($"Вы уверены, что хотите удалить студента '{selectedStudent.FullName}'?",
                                                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            // Если пользователь подтвердил удаление
            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    // Вызываем метод удаления в сервисе данных, передавая ID студента и имя текущего пользователя (для проверки прав)
                    _dataService.DeleteDataObject(selectedStudent.Id, _currentUsername); // Имя метода можно оставить старым, если он универсальный
                    // Обновляем DataGrid
                    LoadUserData();
                }
                catch (DataObjectNotFoundException notFoundEx) // Студент уже удален
                {
                    // Просто логируем и обновляем список, т.к. его уже нет
                    ErrorLogger.LogError(notFoundEx, $"Deleting student data (ID: {selectedStudent.Id}) for user '{_currentUsername}' - Not found (already deleted?)");
                    LoadUserData();
                }
                catch (DataAccessException dataEx) // Ошибка при сохранении изменений после удаления
                {
                    MessageBox.Show($"Ошибка удаления данных студента: {dataEx.Message}", "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(dataEx, $"Deleting student data (ID: {selectedStudent.Id}) for user '{_currentUsername}'");
                }
                catch (InvalidOperationException opEx) // Попытка удалить чужого студента
                {
                    MessageBox.Show(opEx.Message, "Ошибка операции", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ErrorLogger.LogError(opEx, $"Deleting student data (ID: {selectedStudent.Id}) - Permission Denied for {_currentUsername}");
                }
                catch (Exception ex) // Другие ошибки
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при удалении студента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ErrorLogger.LogError(ex, $"Unexpected error deleting student data (ID: {selectedStudent.Id}) for user '{_currentUsername}'");
                }
            }
        }
    }
}