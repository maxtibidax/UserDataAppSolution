using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserDataLibrary.Models; // For StudentData and CourseYear

namespace UserDataLibrary.Services
{
    public enum ReportFormat
    {
        HTML,
        CSV,
        JSON
    }

    public class ReportConfiguration
    {
        public string Title { get; set; } = "Отчет";
        public bool IncludePhotos { get; set; } = true;
        public bool IncludeStatistics { get; set; } = true;
        public string CustomCss { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = Environment.UserName;
    }

    public class ReportService
    {
        private readonly ReportConfiguration _defaultConfig;

        public ReportService(ReportConfiguration defaultConfig = null)
        {
            _defaultConfig = defaultConfig ?? new ReportConfiguration();
        }

        #region HTML Report Generation

        private string GetBaseHtmlShell(string title, string bodyContent, ReportConfiguration config = null)
        {
            config ??= _defaultConfig;
            var customCss = !string.IsNullOrWhiteSpace(config.CustomCss) ? config.CustomCss : GetDefaultCss();

            return $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{EscapeHtml(title)}</title>
    <style>
        {customCss}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{EscapeHtml(title)}</h1>
            <div class=""report-info"">
                <p><small>Создан: {config.GeneratedAt:dd.MM.yyyy HH:mm}</small></p>
                <p><small>Автор: {EscapeHtml(config.GeneratedBy)}</small></p>
            </div>
        </div>
        {bodyContent}
    </div>
</body>
</html>";
        }

        private string GetDefaultCss()
        {
            return @"
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background-color: #f8f9fa; color: #333; line-height: 1.6; }
        .container { background-color: #fff; padding: 30px; border-radius: 12px; box-shadow: 0 2px 20px rgba(0,0,0,0.1); max-width: 1200px; margin: 0 auto; }
        .header { margin-bottom: 30px; border-bottom: 3px solid #4CAF50; padding-bottom: 20px; }
        .report-info { margin-top: 10px; color: #666; }
        h1 { color: #2c3e50; margin: 0; font-size: 2.2em; }
        h2 { color: #34495e; border-bottom: 2px solid #4CAF50; padding-bottom: 10px; margin-top: 30px; }
        h3 { color: #34495e; margin-top: 25px; }
        table { width: 100%; border-collapse: collapse; margin-top: 20px; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        th, td { border: none; padding: 12px 15px; text-align: left; }
        th { background: linear-gradient(135deg, #4CAF50, #45a049); color: white; font-weight: 600; text-transform: uppercase; font-size: 0.9em; letter-spacing: 0.5px; }
        tr:nth-child(even) { background-color: #f8f9fa; }
        tr:hover { background-color: #e3f2fd; transition: background-color 0.3s ease; }
        .photo-container { max-width: 150px; max-height: 150px; margin: 5px 0; border: 2px solid #ddd; border-radius: 8px; overflow: hidden; }
        .photo-container img { width: 100%; height: 100%; object-fit: cover; display: block; }
        .summary-section { margin: 20px 0; padding: 20px; background: linear-gradient(135deg, #e3f2fd, #f3e5f5); border-left: 5px solid #2196F3; border-radius: 8px; }
        .summary-section p { margin: 8px 0; font-size: 1.1em; }
        .summary-section strong { color: #1976d2; }
        .statistics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }
        .stat-card { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); text-align: center; border-top: 4px solid #4CAF50; }
        .stat-value { font-size: 2em; font-weight: bold; color: #4CAF50; margin: 10px 0; }
        .stat-label { color: #666; font-size: 0.9em; text-transform: uppercase; letter-spacing: 0.5px; }
        .no-data { text-align: center; color: #666; font-style: italic; padding: 40px; }
        @media (max-width: 768px) {
            .container { padding: 15px; margin: 10px; }
            table { font-size: 0.9em; }
            .statistics-grid { grid-template-columns: 1fr; }
        }
        @media print {
            body { background-color: white; }
            .container { box-shadow: none; padding: 0; }
        }";
        }

        public async Task GenerateSingleStudentReportAsync(StudentData student, string filePath, ReportConfiguration config = null)
        {
            if (student == null) throw new ArgumentNullException(nameof(student));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            config ??= _defaultConfig;
            var sb = new StringBuilder();

            sb.AppendLine("<h2>📋 Детальная информация по студенту</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Характеристика</th><th>Значение</th></tr>");

            var studentInfo = new Dictionary<string, string>
            {
                ["ID"] = student.Id.ToString(),
                ["ФИО"] = student.FullName,
                ["Группа"] = student.Group,
                ["Email"] = student.Email,
                ["Рейтинг"] = $"{student.Rating:F2}",
                ["Курс"] = student.CurrentCourseYear.ToString(),
                ["Дата зачисления"] = student.EnrollmentDate.ToString("dd.MM.yyyy"),
                ["Время зачисления"] = student.EnrollmentTime.ToString(@"hh\:mm\:ss"),
                ["Получает стипендию"] = student.ReceivesScholarship ? "✅ Да" : "❌ Нет",
                ["Владелец записи"] = student.OwnerUsername
            };

            foreach (var info in studentInfo)
            {
                sb.AppendLine($"<tr><td><strong>{EscapeHtml(info.Key)}</strong></td><td>{EscapeHtml(info.Value)}</td></tr>");
            }

            if (config.IncludePhotos && !string.IsNullOrWhiteSpace(student.PhotoBase64))
            {
                sb.AppendLine("<tr><td><strong>Фотография</strong></td><td>");
                sb.AppendLine("<div class=\"photo-container\">");
                sb.AppendLine($"<img src=\"data:image/jpeg;base64,{student.PhotoBase64}\" alt=\"Фото студента {EscapeHtml(student.FullName)}\">");
                sb.AppendLine("</div></td></tr>");
            }
            else if (config.IncludePhotos)
            {
                sb.AppendLine("<tr><td><strong>Фотография</strong></td><td><em>Отсутствует</em></td></tr>");
            }

            sb.AppendLine("</table>");

            string reportTitle = $"Отчет по студенту: {student.FullName}";
            string htmlContent = GetBaseHtmlShell(reportTitle, sb.ToString(), config);

            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
        }

        public async Task GenerateMultipleStudentsReportAsync(IEnumerable<StudentData> students, string filePath, ReportConfiguration config = null)
        {
            var studentList = students?.ToList();
            if (studentList == null || !studentList.Any())
                throw new ArgumentException("Список студентов не может быть пустым.", nameof(students));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            config ??= _defaultConfig;
            var sb = new StringBuilder();

            if (config.IncludeStatistics)
            {
                sb.AppendLine(GenerateQuickStats(studentList));
            }

            sb.AppendLine("<h2>📊 Список выбранных студентов</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ID</th><th>ФИО</th><th>Группа</th><th>Курс</th><th>Рейтинг</th><th>Стипендия</th><th>Владелец</th></tr>");

            foreach (var student in studentList.OrderBy(s => s.FullName))
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{student.Id}</td>");
                sb.AppendLine($"<td><strong>{EscapeHtml(student.FullName)}</strong></td>");
                sb.AppendLine($"<td>{EscapeHtml(student.Group)}</td>");
                sb.AppendLine($"<td>{student.CurrentCourseYear}</td>");
                sb.AppendLine($"<td>{student.Rating:F2}</td>");
                sb.AppendLine($"<td>{(student.ReceivesScholarship ? "✅" : "❌")}</td>");
                sb.AppendLine($"<td>{EscapeHtml(student.OwnerUsername)}</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");

            string reportTitle = $"Отчет по выбранным студентам ({studentList.Count})";
            string htmlContent = GetBaseHtmlShell(reportTitle, sb.ToString(), config);

            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
        }

        public async Task GenerateAggregateReportAsync(IEnumerable<StudentData> students, string filePath, ReportConfiguration config = null)
        {
            var studentList = students?.ToList();
            if (studentList == null || !studentList.Any())
                throw new ArgumentException("Список студентов не может быть пустым.", nameof(students));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            config ??= _defaultConfig;
            var sb = new StringBuilder();

            sb.AppendLine("<h2>📈 Сводный отчет по выбранным студентам</h2>");

            // Generate statistics cards
            sb.AppendLine(GenerateStatisticsCards(studentList));

            // Course distribution
            sb.AppendLine(GenerateCourseDistribution(studentList));

            // Group distribution
            sb.AppendLine(GenerateGroupDistribution(studentList));

            // Rating analysis
            sb.AppendLine(GenerateRatingAnalysis(studentList));

            string reportTitle = "Сводный отчет по студентам";
            string htmlContent = GetBaseHtmlShell(reportTitle, sb.ToString(), config);

            await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
        }

        #endregion

        #region Statistics Generation

        private string GenerateQuickStats(List<StudentData> students)
        {
            var totalStudents = students.Count;
            var averageRating = students.Average(s => s.Rating);
            var scholarshipCount = students.Count(s => s.ReceivesScholarship);

            return $@"
            <div class=""summary-section"">
                <p><strong>📊 Краткая статистика:</strong></p>
                <p><strong>Всего студентов:</strong> {totalStudents}</p>
                <p><strong>Средний рейтинг:</strong> {averageRating:F2}</p>
                <p><strong>Получают стипендию:</strong> {scholarshipCount} ({(double)scholarshipCount / totalStudents * 100:F1}%)</p>
            </div>";
        }

        private string GenerateStatisticsCards(List<StudentData> students)
        {
            var totalStudents = students.Count;
            var averageRating = students.Average(s => s.Rating);
            var scholarshipCount = students.Count(s => s.ReceivesScholarship);
            var maxRating = students.Max(s => s.Rating);
            var minRating = students.Min(s => s.Rating);

            return $@"
            <div class=""statistics-grid"">
                <div class=""stat-card"">
                    <div class=""stat-value"">{totalStudents}</div>
                    <div class=""stat-label"">Всего студентов</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-value"">{averageRating:F2}</div>
                    <div class=""stat-label"">Средний рейтинг</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-value"">{scholarshipCount}</div>
                    <div class=""stat-label"">Стипендиатов</div>
                </div>
                <div class=""stat-card"">
                    <div class=""stat-value"">{maxRating:F2}</div>
                    <div class=""stat-label"">Максимальный рейтинг</div>
                </div>
            </div>";
        }

        private string GenerateCourseDistribution(List<StudentData> students)
        {
            var sb = new StringBuilder();
            var studentsByCourse = students
                .GroupBy(s => s.CurrentCourseYear)
                .Select(g => new { Course = g.Key, Count = g.Count(), Percentage = (double)g.Count() / students.Count * 100 })
                .OrderBy(x => x.Course)
                .ToList();

            sb.AppendLine("<h3>🎓 Распределение по курсам</h3>");

            if (studentsByCourse.Any())
            {
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Курс</th><th>Количество</th><th>Процент</th><th>Средний рейтинг</th></tr>");

                foreach (var courseGroup in studentsByCourse)
                {
                    var courseStudents = students.Where(s => s.CurrentCourseYear == courseGroup.Course);
                    var avgRating = courseStudents.Average(s => s.Rating);

                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td><strong>{courseGroup.Course} курс</strong></td>");
                    sb.AppendLine($"<td>{courseGroup.Count}</td>");
                    sb.AppendLine($"<td>{courseGroup.Percentage:F1}%</td>");
                    sb.AppendLine($"<td>{avgRating:F2}</td>");
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<div class=\"no-data\">Нет данных для отображения распределения по курсам.</div>");
            }

            return sb.ToString();
        }

        private string GenerateGroupDistribution(List<StudentData> students)
        {
            var sb = new StringBuilder();
            var studentsByGroup = students
                .GroupBy(s => s.Group)
                .Select(g => new { Group = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10) // Top 10 groups
                .ToList();

            sb.AppendLine("<h3>👥 Распределение по группам (топ-10)</h3>");

            if (studentsByGroup.Any())
            {
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Группа</th><th>Количество студентов</th></tr>");

                foreach (var group in studentsByGroup)
                {
                    sb.AppendLine($"<tr><td><strong>{EscapeHtml(group.Group)}</strong></td><td>{group.Count}</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            return sb.ToString();
        }

        private string GenerateRatingAnalysis(List<StudentData> students)
        {
            var sb = new StringBuilder();
            var ratingRanges = new[]
            {
                new { Name = "Отличники (4.5-5.0)", Min = 4.5, Max = 5.0 },
                new { Name = "Хорошисты (3.5-4.49)", Min = 3.5, Max = 4.49 },
                new { Name = "Удовлетворительно (2.5-3.49)", Min = 2.5, Max = 3.49 },
                new { Name = "Неудовлетворительно (0-2.49)", Min = 0.0, Max = 2.49 }
            };

            sb.AppendLine("<h3>⭐ Анализ рейтингов</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Категория</th><th>Количество</th><th>Процент</th></tr>");

            foreach (var range in ratingRanges)
            {
                var count = students.Count(s => s.Rating >= range.Min && s.Rating <= range.Max);
                var percentage = (double)count / students.Count * 100;

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td><strong>{range.Name}</strong></td>");
                sb.AppendLine($"<td>{count}</td>");
                sb.AppendLine($"<td>{percentage:F1}%</td>");
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        #endregion

        #region Utility Methods

        private static string EscapeHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;");
        }

        public async Task<bool> ValidateFilePathAsync(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Synchronous Wrappers (для обратной совместимости)

        public void GenerateSingleStudentReport(StudentData student, string filePath, ReportConfiguration config = null)
        {
            GenerateSingleStudentReportAsync(student, filePath, config).GetAwaiter().GetResult();
        }

        public void GenerateMultipleStudentsReport(IEnumerable<StudentData> students, string filePath, ReportConfiguration config = null)
        {
            GenerateMultipleStudentsReportAsync(students, filePath, config).GetAwaiter().GetResult();
        }

        public void GenerateAggregateReport(IEnumerable<StudentData> students, string filePath, ReportConfiguration config = null)
        {
            GenerateAggregateReportAsync(students, filePath, config).GetAwaiter().GetResult();
        }

        #endregion
    }
}