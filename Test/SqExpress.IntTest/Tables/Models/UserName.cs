using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Tables.Models
{
    public class UserName
    {
        public UserName(EntUser id, string firstName, string lastName)
        {
            this.Id = id;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public static UserName Read(ISqDataRecordReader record, TableItUser table)
        {
            return new UserName(id: (EntUser)table.UserId.Read(record), firstName: table.FirstName.Read(record), lastName: table.LastName.Read(record));
        }

        public EntUser Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public static TableColumn[] GetColumns(TableItUser table)
        {
            return new TableColumn[]{table.UserId, table.FirstName, table.LastName};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItUser, UserName> s)
        {
            return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItUser, UserName> s)
        {
            return s.Set(s.Target.UserId, (int)s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItUser, UserName> s)
        {
            return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
        }

        public UserName WithId(EntUser id)
        {
            return new UserName(id: id, firstName: this.FirstName, lastName: this.LastName);
        }

        public UserName WithFirstName(string firstName)
        {
            return new UserName(id: this.Id, firstName: firstName, lastName: this.LastName);
        }

        public UserName WithLastName(string lastName)
        {
            return new UserName(id: this.Id, firstName: this.FirstName, lastName: lastName);
        }
    }
}