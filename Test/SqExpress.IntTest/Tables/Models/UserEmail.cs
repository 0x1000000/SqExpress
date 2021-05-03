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

        public UserEmail WithId(EntUser id)
        {
            return new UserEmail(id: id, email: this.Email);
        }

        public UserEmail WithEmail(string email)
        {
            return new UserEmail(id: this.Id, email: email);
        }

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

        public static ISqModelReader<UserEmail, TableItUser> GetReader()
        {
            return UserEmailReader.Instance;
        }

        private class UserEmailReader : ISqModelReader<UserEmail, TableItUser>
        {
            public static UserEmailReader Instance { get; } = new UserEmailReader();
            TableColumn[] ISqModelReader<UserEmail, TableItUser>.GetColumns(TableItUser table)
            {
                return UserEmail.GetColumns(table);
            }

            UserEmail ISqModelReader<UserEmail, TableItUser>.Read(ISqDataRecordReader record, TableItUser table)
            {
                return UserEmail.Read(record, table);
            }
        }

        public static ISqModelUpdaterKey<UserEmail, TableItUser> GetUpdater()
        {
            return UserEmailUpdater.Instance;
        }

        private class UserEmailUpdater : ISqModelUpdaterKey<UserEmail, TableItUser>
        {
            public static UserEmailUpdater Instance { get; } = new UserEmailUpdater();
            IRecordSetterNext ISqModelUpdater<UserEmail, TableItUser>.GetMapping(IDataMapSetter<TableItUser, UserEmail> dataMapSetter)
            {
                return UserEmail.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<UserEmail, TableItUser>.GetUpdateKeyMapping(IDataMapSetter<TableItUser, UserEmail> dataMapSetter)
            {
                return UserEmail.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<UserEmail, TableItUser>.GetUpdateMapping(IDataMapSetter<TableItUser, UserEmail> dataMapSetter)
            {
                return UserEmail.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}