using System;

namespace UserDataLibrary.Exceptions
{
    // Можно наследовать от AuthenticationException или просто Exception
    public class UserAlreadyExistsException : Exception
    {
        public string Username { get; }

        public UserAlreadyExistsException(string username)
            : base($"Пользователь с именем '{username}' уже существует.")
        {
            Username = username;
        }

        public UserAlreadyExistsException(string username, string message) : base(message)
        {
            Username = username;
        }

        public UserAlreadyExistsException(string username, string message, Exception innerException) : base(message, innerException)
        {
            Username = username;
        }
    }
}