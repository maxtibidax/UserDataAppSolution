using System;

namespace UserDataLibrary.Exceptions
{
    public class UserNotFoundException : AuthenticationException
    {
        public UserNotFoundException(string username) : base($"Пользователь '{username}' не найден.") { }
    }
}