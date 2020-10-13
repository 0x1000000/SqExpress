using System;

namespace SqExpress.DataAccess
{
    public class SqDatabaseCommandException: Exception
    {
        public string CommandText { get; }

        public SqDatabaseCommandException(string commandText, string message, Exception innerException) : base(message, innerException)
        {
            this.CommandText = commandText;
        }
    }
}