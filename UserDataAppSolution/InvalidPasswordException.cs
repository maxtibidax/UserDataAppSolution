using System;

namespace UserDataLibrary.Exceptions
{
    public class InvalidPasswordException : AuthenticationException
    {
        public InvalidPasswordException(string username) : base($"Неверный пароль для пользователя '{username}'.") { }
    }
}