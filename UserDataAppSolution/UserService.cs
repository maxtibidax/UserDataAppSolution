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

        public UserService(string baseDirectory)
        {
            _usersFilePath = Path.Combine(baseDirectory, FileConstants.UsersFileName);
            EnsureUserFileExists(); // Создаем файл, если его нет
        }

        private void EnsureUserFileExists()
        {
            try
            {
                if (!File.Exists(_usersFilePath))
                {
                    // Создаем файл с примером пользователя admin/admin
                    File.WriteAllLines(_usersFilePath, new[] { "admin:admin" });
                }
            }
            catch (Exception ex)
            {
                // Не кидаем исключение при инициализации, чтобы приложение могло запуститься
                // Но можно логировать эту ошибку, если нужен контроль создания файла
                Console.WriteLine($"Warning: Could not ensure user file exists at {_usersFilePath}. Error: {ex.Message}");
            }
        }


        private List<User> LoadUsers()
        {
            var users = new List<User>();
            try
            {
                if (!File.Exists(_usersFilePath))
                {
                    // Можно создать пустой файл или файл с админом по умолчанию
                    // File.WriteAllText(_usersFilePath, "admin:admin"); // Пример
                    return users; // Возвращаем пустой список, если файла нет
                }

                var lines = File.ReadAllLines(_usersFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        users.Add(new User { Username = parts[0].Trim(), Password = parts[1] }); // Пароль хранится открыто!
                    }
                    // Можно добавить обработку некорректных строк
                }
            }
            catch (IOException ex)
            {
                throw new DataAccessException($"Ошибка чтения файла пользователей: {_usersFilePath}", ex);
            }
            catch (Exception ex) // Другие возможные ошибки (права доступа и т.д.)
            {
                throw new DataAccessException($"Неожиданная ошибка при загрузке пользователей.", ex);
            }
            return users;
        }

        public User Authenticate(string username, string password)
        {
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
    }
}