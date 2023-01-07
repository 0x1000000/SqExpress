using System;

namespace SqExpress.Test
{
    public class UserData
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public DateTime RegDate { get; set; }
    }
}