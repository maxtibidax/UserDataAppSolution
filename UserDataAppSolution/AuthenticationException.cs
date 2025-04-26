using System;

namespace UserDataLibrary.Exceptions
{
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }
}