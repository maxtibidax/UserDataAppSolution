using System;

namespace UserDataLibrary.Models
{
    public class StudentData // Переименовали класс
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Уникальный идентификатор
        public string OwnerUsername { get; set; } // Имя пользователя, который добавил студента

        // --- Поля студента ---
        public string FullName { get; set; } // ФИО
        public string Group { get; set; } // Группа
        public string Email { get; set; } // Email
        public double Rating { get; set; } // Рейтинг (можно использовать decimal для точности)
        public DateTime EnrollmentDate { get; set; } = DateTime.Today; // Дата зачисления
        public TimeSpan EnrollmentTime { get; set; } // Время зачисления
        public bool ReceivesScholarship { get; set; } // Получает стипендию (переключатель)
        public CourseYear CurrentCourseYear { get; set; } // Курс (перечисление)
        public string PhotoBase64 { get; set; } // Фотография в Base64

        // Конструктор по умолчанию для сериализации
        public StudentData() { }

        // Удобный конструктор (можно добавить больше параметров)
        public StudentData(string ownerUsername, string fullName, string group)
        {
            OwnerUsername = ownerUsername;
            FullName = fullName;
            Group = group;
            // Можно установить значения по умолчанию для других полей здесь, если нужно
            EnrollmentDate = DateTime.Today;
            CurrentCourseYear = CourseYear.First;
        }
    }
}