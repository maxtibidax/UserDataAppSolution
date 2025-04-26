using System;

namespace UserDataLibrary.Exceptions
{
    public class DataObjectNotFoundException : Exception
    {
        public DataObjectNotFoundException(Guid id)
            : base($"Объект данных с ID '{id}' не найден.") { }
    }
}