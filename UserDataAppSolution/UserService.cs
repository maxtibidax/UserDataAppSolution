using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UserDataLibrary.Exceptions;
using UserDataLibrary.Models;

namespace UserDataLibrary.Services
{
    public class UserService
    {
        private readonly string _usersFilePath;
        private static readonly object _userFileLock = new object(); // Блокировка для файла пользователей

        public UserService(string baseDirectory)
        {
            _usersFilePath = Path.Combine(baseDirectory, FileConstants.UsersFileName);
            EnsureUserFileExists();
        }

        private void EnsureUserFileExists()
        {
            // Используем блокировку и здесь на случай параллельного доступа при первом запуске
            lock (_userFileLock)
            {
                try
                {
                    if (!File.Exists(_usersFilePath))
                    {
                        // Создаем файл с примером пользователя admin:admin
                        // Используем WriteAllText для простоты, если файла нет
                        File.WriteAllText(_usersFilePath, "admin:admin" + Environment.NewLine);
                    }
                }
                catch (IOException ex)
                {
                    // Логируем или обрабатываем ошибку создания файла (но не бросаем исключение, чтобы не сломать конструктор)
                    Console.WriteLine($"Warning: Could not create user file at '{_usersFilePath}'. Error: {ex.Message}");
                    // Можно пробросить специфическое исключение, если создание файла критично
                    // throw new DataAccessException($"Не удалось создать файл пользователей '{_usersFilePath}'.", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Warning: No permission to create user file at '{_usersFilePath}'. Error: {ex.Message}");
                    // throw new DataAccessException($"Нет прав на создание файла пользователей '{_usersFilePath}'.", ex);
                }
                catch (Exception ex) // Другие ошибки
                {
                    Console.WriteLine($"Warning: Unexpected error ensuring user file exists at '{_usersFilePath}'. Error: {ex.Message}");
                    // throw new DataAccessException($"Неожиданная ошибка при проверке/создании файла пользователей '{_usersFilePath}'.", ex);
                }
            }
        }

        // Метод загрузки пользователей (оставляем без изменений)
        private List<User> LoadUsers()
        {
            var users = new List<User>();
            lock (_userFileLock) // Блокируем чтение
            {
                try
                {
                    if (!File.Exists(_usersFilePath))
                    {
                        return users;
                    }

                    // Используем ReadLines для ленивого чтения, если файл большой
                    var lines = File.ReadLines(_usersFilePath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue; // Пропускаем пустые строки

                        var parts = line.Split(':');
                        // Проверяем, что есть ровно две части и имя пользователя не пустое
                        if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                        {
                            users.Add(new User { Username = parts[0].Trim(), Password = parts[1] }); // Пароль хранится открыто!
                        }
                        else
                        {
                            // Можно логировать некорректные строки
                            Console.WriteLine($"Warning: Skipped invalid line in user file: '{line}'");
                        }
                    }
                }
                catch (IOException ex)
                {
                    throw new DataAccessException($"Ошибка чтения файла пользователей: '{_usersFilePath}'.", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new DataAccessException($"Отсутствуют права доступа для чтения файла пользователей: '{_usersFilePath}'.", ex);
                }
                catch (Exception ex)
                {
                    throw new DataAccessException($"Неожиданная ошибка при загрузке пользователей из '{_usersFilePath}'.", ex);
                }
            } // lock
            return users;
        }

        // Метод аутентификации (оставляем без изменений)
        public User Authenticate(string username, string password)
        {
            // ... (код метода Authenticate остается прежним) ...
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                throw new AuthenticationException("Имя пользователя и пароль не могут быть пустыми.");
            }

            List<User> users;
            try
            {
                users = LoadUsers();
            }
            catch (DataAccessException) // Перебрасываем ошибку доступа к данным
            {
                throw;
            }

            var user = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                throw new UserNotFoundException(username);
            }

            // Прямое сравнение паролей (НЕБЕЗОПАСНО!)
            if (user.Password != password)
            {
                throw new InvalidPasswordException(username);
            }

            return user; // Возвращаем пользователя при успехе
        }

        // НОВЫЙ МЕТОД для добавления пользователя
        public void AddUser(string username, string password)
        {
            // 1. Валидация входных данных
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым.", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не может быть пустым.", nameof(password));
            }
            // Дополнительные проверки (длина, символы и т.д.) можно добавить здесь
            if (username.Contains(':')) // Двоеточие используется как разделитель
            {
                throw new ArgumentException("Имя пользователя не может содержать символ ':'.", nameof(username));
            }

            // Используем блокировку на весь процесс проверки и добавления
            lock (_userFileLock)
            {
                // 2. Проверка на существование пользователя
                // Загружаем текущих пользователей внутри блокировки, чтобы избежать гонки состояний
                List<User> currentUsers;
                try
                {
                    currentUsers = LoadUsers(); // LoadUsers уже использует блокировку, но повторная не помешает
                }
                catch (DataAccessException) // Если не можем прочитать файл, не можем и добавить
                {
                    throw; // Перебрасываем ошибку доступа
                }

                if (currentUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new UserAlreadyExistsException(username);
                }

                // 3. Добавление пользователя в файл
                try
                {
                    // Формируем строку для добавления
                    string newUserLine = $"{username.Trim()}:{password}"; // Убираем лишние пробелы из имени
                    // Добавляем строку в конец файла. AppendAllText добавляет перенос строки, если его нет.
                    // Используем AppendAllLines для гарантии переноса строки.
                    File.AppendAllLines(_usersFilePath, new[] { newUserLine });

                    // Альтернатива: File.AppendAllText(_usersFilePath, newUserLine + Environment.NewLine);
                }
                catch (IOException ex)
                {
                    throw new DataAccessException($"Ошибка записи в файл пользователей '{_usersFilePath}'.", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new DataAccessException($"Отсутствуют права доступа для записи в файл пользователей '{_usersFilePath}'.", ex);
                }
                catch (Exception ex)
                {
                    throw new DataAccessException($"Неожиданная ошибка при добавлении пользователя в файл '{_usersFilePath}'.", ex);
                }
            } // lock
        }
    }
}