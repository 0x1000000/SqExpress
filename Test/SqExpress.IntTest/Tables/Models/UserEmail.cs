using System.Text.Json.Serialization;
using SqExpress.QueryBuilders.RecordSetter;

namespace SqExpress.IntTest.Tables.Models
{
    public class UserEmail
    {
        public UserEmail(EntUser id, string email)
        {
            this.Id = id;
            this.Email = email;
        }

        public static UserEmail Read(ISqDataRecordReader record, TableItUser table)
        {
            return new UserEmail(id: (EntUser)table.UserId.Read(record), email: table.Email.Read(record));
        }

        [JsonPropertyName("id")]
        public EntUser Id { get; }

        [JsonPropertyName("email")]
        public string Email { get; }

        public static TableColumn[] GetColumns(TableItUser table)
        {
            return new TableColumn[]{table.UserId, table.Email};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItUser, UserEmail> s)
        {
            return s.Set(s.Target.Email, s.Source.Email);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItUser, UserEmail> s)
        {
            return s.Set(s.Target.UserId, (int)s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItUser, UserEmail> s)
        {
            return s.Set(s.Target.Email, s.Source.Email);
        }

        public UserEmail WithId(EntUser id)
        {
            return new UserEmail(id: id, email: this.Email);
        }

        public UserEmail WithEmail(string email)
        {
            return new UserEmail(id: this.Id, email: email);
        }
    }
}