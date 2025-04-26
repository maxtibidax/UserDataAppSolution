using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json; // Используем System.Text.Json
using UserDataLibrary.Exceptions;
using UserDataLibrary.Models; // Убедитесь, что импортирована модель StudentData

namespace UserDataLibrary.Services
{
    public class DataService
    {
        private readonly string _dataFilePath;
        private List<StudentData> _allStudentData; // Хранилище данных студентов в памяти
        private static readonly object _fileLock = new object(); // Объект для блокировки доступа к файлу (базовая потокобезопасность)

        // Параметры для сериализации/десериализации JSON
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true, // Делает JSON файл читаемым
            PropertyNameCaseInsensitive = true // Не учитывать регистр имен свойств при чтении JSON
            // Дополнительные настройки можно добавить здесь (например, конвертеры)
        };

        public DataService(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("Base directory cannot be null or empty.", nameof(baseDirectory));
            }
            // Формируем полный путь к файлу данных
            _dataFilePath = Path.Combine(baseDirectory, FileConstants.DataFileName);
            // Загружаем данные при создании сервиса
            _allStudentData = LoadData();
        }

        // Загружает данные из файла data.json
        private List<StudentData> LoadData()
        {
            // Блокируем доступ к файлу на время чтения
            lock (_fileLock)
            {
                try
                {
                    // Если файл не существует, возвращаем новый пустой список
                    if (!File.Exists(_dataFilePath))
                    {
                        return new List<StudentData>();
                    }

                    // Читаем весь текст из файла
                    string json = File.ReadAllText(_dataFilePath);

                    // Если файл пустой или содержит только пробелы, возвращаем пустой список
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new List<StudentData>();
                    }

                    // Десериализуем JSON в список объектов StudentData
                    var data = JsonSerializer.Deserialize<List<StudentData>>(json, _jsonOptions);

                    // Возвращаем десериализованные данные или новый пустой список, если десериализация вернула null
                    return data ?? new List<StudentData>();
                }
                // Ловим ошибки, связанные с некорректным форматом JSON
                catch (JsonException ex)
                {
                    throw new DataAccessException($"Ошибка чтения или разбора файла данных '{_dataFilePath}'. Некорректный формат JSON.", ex);
                }
                // Ловим ошибки ввода-вывода (например, файл занят, нет прав доступа)
                catch (IOException ex)
                {
                    throw new DataAccessException($"Ошибка чтения файла данных '{_dataFilePath}'.", ex);
                }
                // Ловим другие возможные ошибки при доступе к файлу
                catch (UnauthorizedAccessException ex)
                {
                    throw new DataAccessException($"Отсутствуют права доступа для чтения файла данных '{_dataFilePath}'.", ex);
                }
                // Ловим прочие непредвиденные ошибки
                catch (Exception ex)
                {
                    throw new DataAccessException($"Неожиданная ошибка при загрузке данных из файла '{_dataFilePath}'.", ex);
                }
            } // lock
        }

        // Сохраняет текущий список данных (_allStudentData) в файл data.json
        private void SaveData()
        {
            // Блокируем доступ к файлу на время записи
            lock (_fileLock)
            {
                try
                {
                    // Сериализуем текущий список студентов в JSON строку
                    string json = JsonSerializer.Serialize(_allStudentData, _jsonOptions);
                    // Записываем JSON строку в файл, перезаписывая его содержимое
                    File.WriteAllText(_dataFilePath, json);
                }
                // Ловим ошибки, связанные с процессом сериализации
                catch (JsonException ex)
                {
                    // Эта ошибка маловероятна при сериализации стандартных типов, но лучше ее обработать
                    throw new DataAccessException("Ошибка преобразования данных студентов в формат JSON.", ex);
                }
                // Ловим ошибки ввода-вывода при записи в файл
                catch (IOException ex)
                {
                    throw new DataAccessException($"Ошибка записи в файл данных '{_dataFilePath}'.", ex);
                }
                // Ловим другие возможные ошибки при доступе к файлу
                catch (UnauthorizedAccessException ex)
                {
                    throw new DataAccessException($"Отсутствуют права доступа для записи в файл данных '{_dataFilePath}'.", ex);
                }
                // Ловим прочие непредвиденные ошибки
                catch (Exception ex)
                {
                    throw new DataAccessException($"Неожиданная ошибка при сохранении данных в файл '{_dataFilePath}'.", ex);
                }
            } // lock
            // Примечание: Блокировка (_fileLock) обеспечивает базовую потокобезопасность на уровне операций чтения/записи файла.
            // Однако, если несколько потоков одновременно модифицируют список _allStudentData *между* операциями,
            // могут возникнуть гонки данных. Для полной потокобезопасности сервиса может потребоваться
            // более гранулярная блокировка или использование потокобезопасных коллекций.
            // Для данного WPF приложения с одним UI потоком это обычно не является проблемой.
        }

        // Возвращает список студентов, принадлежащих указанному пользователю
        public List<StudentData> GetDataForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                // Можно вернуть пустой список или бросить исключение, в зависимости от логики
                return new List<StudentData>();
                // throw new ArgumentException("Username cannot be null or empty", nameof(username));
            }

            // Фильтруем общий список студентов по OwnerUsername (без учета регистра)
            // ВАЖНО: .ToList() создает КОПИЮ списка для возврата,
            // чтобы внешние изменения не затрагивали внутренний кэш _allStudentData.
            return _allStudentData
                .Where(s => s.OwnerUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Добавляет нового студента в хранилище
        public void AddStudentData(StudentData newItem)
        {
            // Проверка входного параметра
            if (newItem == null)
            {
                throw new ArgumentNullException(nameof(newItem), "Данные нового студента не могут быть null.");
            }
            // Проверка обязательного поля (владельца)
            if (string.IsNullOrWhiteSpace(newItem.OwnerUsername))
            {
                throw new ArgumentException("OwnerUsername не может быть пустым при добавлении студента.", nameof(newItem));
            }

            // Генерируем новый уникальный ID для записи, игнорируя тот, что мог быть в newItem
            newItem.Id = Guid.NewGuid();

            // Добавляем новый элемент в список в памяти
            _allStudentData.Add(newItem);

            // Сохраняем обновленный список в файл
            SaveData();
        }

        // Обновляет данные существующего студента
        public void UpdateStudentData(StudentData updatedItem)
        {
            // Проверка входного параметра
            if (updatedItem == null)
            {
                throw new ArgumentNullException(nameof(updatedItem), "Данные для обновления студента не могут быть null.");
            }

            // Ищем существующую запись по ID
            var existingItem = _allStudentData.FirstOrDefault(s => s.Id == updatedItem.Id);

            // Если запись с таким ID не найдена, генерируем исключение
            if (existingItem == null)
            {
                // Используем DataObjectNotFoundException, как договорились ранее
                throw new DataObjectNotFoundException(updatedItem.Id);
                // Или можно создать StudentDataNotFoundException(updatedItem.Id)
            }

            // Проверка смены владельца (опционально, но может быть полезно)
            // if (!existingItem.OwnerUsername.Equals(updatedItem.OwnerUsername, StringComparison.OrdinalIgnoreCase))
            // {
            //     throw new InvalidOperationException("Изменение владельца записи не допускается.");
            // }

            // Находим индекс существующего элемента в списке
            int index = _allStudentData.IndexOf(existingItem);

            // Заменяем старый объект в списке на обновленный
            // Это необходимо, так как updatedItem - это потенциально другой объект (копия),
            // пришедший из UI после редактирования.
            if (index != -1) // Дополнительная проверка, что элемент все еще в списке
            {
                _allStudentData[index] = updatedItem;
            }
            else
            {
                // Этого не должно произойти, если FirstOrDefault нашел элемент, но на всякий случай
                throw new InvalidOperationException($"Элемент с ID {updatedItem.Id} был найден, но его индекс не определен. Произошла ошибка синхронизации данных.");
            }


            // Сохраняем изменения в файл
            SaveData();
        }

        // Удаляет студента по его ID
        public void DeleteDataObject(Guid id, string currentUsername) // Оставляем имя метода для совместимости с MainWindow
        {
            // Проверка имени пользователя
            if (string.IsNullOrWhiteSpace(currentUsername))
            {
                throw new ArgumentException("Имя текущего пользователя не может быть пустым для удаления.", nameof(currentUsername));
            }

            // Ищем элемент для удаления по ID
            var itemToRemove = _allStudentData.FirstOrDefault(s => s.Id == id);

            // Если элемент не найден, ничего не делаем (или можно бросить исключение DataObjectNotFoundException)
            if (itemToRemove == null)
            {
                // Можно залогировать, что попытка удалить несуществующий объект
                // Console.WriteLine($"Attempted to delete non-existent student data with ID: {id}");
                return; // Просто выходим
                // throw new DataObjectNotFoundException(id); // Альтернатива - бросить исключение
            }

            // Проверка прав: убеждаемся, что текущий пользователь является владельцем записи
            if (!itemToRemove.OwnerUsername.Equals(currentUsername, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Пользователь '{currentUsername}' не имеет прав на удаление записи (ID: {id}), принадлежащей '{itemToRemove.OwnerUsername}'.");
            }

            // Удаляем элемент из списка в памяти
            _allStudentData.Remove(itemToRemove);

            // Сохраняем изменения в файл
            SaveData();
        }
    }
}