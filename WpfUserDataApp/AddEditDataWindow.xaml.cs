using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions; // Для валидации Email
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UserDataLibrary.Helpers;
using UserDataLibrary.Models;
using WpfUserDataApp.Utils;

namespace WpfUserDataApp
{
    public partial class AddEditDataWindow : Window
    {
        // Изменили имя свойства и тип
        public StudentData CurrentStudentData { get; private set; }
        private readonly bool _isEditMode;
        private readonly string _ownerUsername;
        private string _photoBase64Holder; // Переименовали

        // Конструктор для добавления
        public AddEditDataWindow(string ownerUsername)
        {
            InitializeComponent();
            _isEditMode = false;
            _ownerUsername = ownerUsername;
            // Создаем новый объект StudentData
            CurrentStudentData = new StudentData { OwnerUsername = _ownerUsername };
            Title = "Добавить студента";
            PopulateControls();
        }

        // Конструктор для редактирования
        public AddEditDataWindow(string ownerUsername, StudentData studentToEdit) // Изменили тип
        {
            InitializeComponent();
            _isEditMode = true;
            _ownerUsername = ownerUsername;
            // Работаем с переданным объектом
            CurrentStudentData = studentToEdit;
            Title = "Редактировать студента";
            PopulateControls();
        }

        private void PopulateControls()
        {
            // Заполняем новые поля
            FullNameTextBox.Text = CurrentStudentData.FullName;
            GroupTextBox.Text = CurrentStudentData.Group;
            EmailTextBox.Text = CurrentStudentData.Email;
            // Форматируем рейтинг при отображении
            RatingTextBox.Text = CurrentStudentData.Rating.ToString("F2", CultureInfo.InvariantCulture); // Используем точку как разделитель для ввода/отображения
            EnrollmentDatePicker.SelectedDate = CurrentStudentData.EnrollmentDate;
            EnrollmentTimeTextBox.Text = CurrentStudentData.EnrollmentTime.ToString(@"hh\:mm");
            CourseComboBox.SelectedItem = CurrentStudentData.CurrentCourseYear;
            ReceivesScholarshipCheckBox.IsChecked = CurrentStudentData.ReceivesScholarship;
            _photoBase64Holder = CurrentStudentData.PhotoBase64; // <--- Изменено

            DisplayImageFromBase64(_photoBase64Holder);
        }

        // Переименовали и обновили метод
        private bool UpdateStudentDataFromControls()
        {
            ErrorTextBlockDialog.Text = "";
            bool isValid = true;
            var errors = "";

            // --- Валидация ---
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                errors += "ФИО не может быть пустым.\n";
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(GroupTextBox.Text))
            {
                errors += "Группа не может быть пустой.\n";
                isValid = false;
            }
            // Простая валидация Email
            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) && !Regex.IsMatch(EmailTextBox.Text, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                errors += "Некорректный формат Email.\n";
                isValid = false;
            }
            else if (string.IsNullOrWhiteSpace(EmailTextBox.Text)) // Email можно сделать необязательным? Если да, убрать эту ветку. Если обязателен - оставить.
            {
                errors += "Email не может быть пустым.\n";
                isValid = false;
            }

            double parsedRating;
            // Используем InvariantCulture для разделителя-точки
            if (!double.TryParse(RatingTextBox.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out parsedRating))
            {
                errors += "Некорректный формат рейтинга. Используйте число (разделитель - точка).\n";
                isValid = false;
            }
            // Дополнительная валидация рейтинга (например, диапазон)
            else if (parsedRating < 0 || parsedRating > 100) // Пример диапазона 0-100
            {
                errors += "Рейтинг должен быть в диапазоне от 0 до 100.\n";
                isValid = false;
            }


            if (EnrollmentDatePicker.SelectedDate == null)
            {
                errors += "Необходимо выбрать дату зачисления.\n";
                isValid = false;
            }

            TimeSpan parsedTime;
            if (!TimeSpan.TryParseExact(EnrollmentTimeTextBox.Text, @"hh\:mm", CultureInfo.InvariantCulture, out parsedTime) &&
                !TimeSpan.TryParseExact(EnrollmentTimeTextBox.Text, @"h\:mm", CultureInfo.InvariantCulture, out parsedTime))
            {
                errors += "Неверный формат времени зачисления. Используйте ЧЧ:ММ.\n";
                isValid = false;
            }

            if (CourseComboBox.SelectedItem == null)
            {
                errors += "Необходимо выбрать курс.\n";
                isValid = false;
            }

            if (!isValid)
            {
                ErrorTextBlockDialog.Text = errors;
                return false;
            }

            // --- Обновление объекта ---
            CurrentStudentData.FullName = FullNameTextBox.Text;
            CurrentStudentData.Group = GroupTextBox.Text;
            CurrentStudentData.Email = EmailTextBox.Text;
            CurrentStudentData.Rating = parsedRating; // Используем спарсенное значение
            CurrentStudentData.EnrollmentDate = EnrollmentDatePicker.SelectedDate.Value.Date;
            CurrentStudentData.EnrollmentTime = parsedTime;
            CurrentStudentData.CurrentCourseYear = (CourseYear)CourseComboBox.SelectedItem;
            CurrentStudentData.ReceivesScholarship = ReceivesScholarshipCheckBox.IsChecked ?? false;
            CurrentStudentData.PhotoBase64 = _photoBase64Holder; // <--- Изменено
            CurrentStudentData.OwnerUsername = _ownerUsername;

            return true;
        }


        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика выбора файла остается прежней, но результат сохраняется в _photoBase64Holder
            var openFileDialog = new OpenFileDialog { /* ... */ };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _photoBase64Holder = ImageHelper.ImageFileToBase64(openFileDialog.FileName); // <--- Изменено
                    if (_photoBase64Holder != null)
                    {
                        DisplayImageFromBase64(_photoBase64Holder);
                    }
                    // ... обработка ошибок ...
                }
                catch { }
             }
        }

        private void DisplayImageFromBase64(string base64)
        {
            // Используем поле PhotoBase64 объекта, но для предпросмотра берем из _photoBase64Holder
            try
            {
                PreviewImage.Source = ImageHelper.Base64ToImageSource(base64); // Используем переданный base64
            }
            catch { }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UpdateStudentDataFromControls()) // Вызываем обновленный метод
            {
                DialogResult = true;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}