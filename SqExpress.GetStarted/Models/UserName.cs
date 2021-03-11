using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;

namespace SqExpress.GetStarted.Models
{
    public class UserName
    {
        public UserName(int id, string firstName, string lastName)
        {
            this.Id = id;
            this.FirstName = firstName;
            this.LastName = lastName;
        }

        public static UserName Read(ISqDataRecordReader record, TableUser table)
        {
            return new UserName(id: table.UserId.Read(record), firstName: table.FirstName.Read(record), lastName: table.LastName.Read(record));
        }

        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

        public static TableColumn[] GetColumns(TableUser table)
        {
            return new TableColumn[]{table.UserId, table.FirstName, table.LastName};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableUser, UserName> s)
        {
            return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableUser, UserName> s)
        {
            return s.Set(s.Target.UserId, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableUser, UserName> s)
        {
            return s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName);
        }

        public UserName WithId(int id)
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