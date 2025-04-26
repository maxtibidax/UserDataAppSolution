using System;

namespace UserDataLibrary.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}