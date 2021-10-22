using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

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

        public static UserName ReadOrdinal(ISqDataRecordReader record, TableUser table, int offset)
        {
            return new UserName(id: table.UserId.Read(record, offset), firstName: table.FirstName.Read(record, offset + 1), lastName: table.LastName.Read(record, offset + 2));
        }

        public int Id { get; }

        public string FirstName { get; }

        public string LastName { get; }

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

        public static ISqModelReader<UserName, TableUser> GetReader()
        {
            return UserNameReader.Instance;
        }

        private class UserNameReader : ISqModelReader<UserName, TableUser>
        {
            public static UserNameReader Instance { get; } = new UserNameReader();
            IReadOnlyList<ExprColumn> ISqModelReader<UserName, TableUser>.GetColumns(TableUser table)
            {
                return UserName.GetColumns(table);
            }

            UserName ISqModelReader<UserName, TableUser>.Read(ISqDataRecordReader record, TableUser table)
            {
                return UserName.Read(record, table);
            }

            UserName ISqModelReader<UserName, TableUser>.ReadOrdinal(ISqDataRecordReader record, TableUser table, int offset)
            {
                return UserName.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<UserName, TableUser> GetUpdater()
        {
            return UserNameUpdater.Instance;
        }

        private class UserNameUpdater : ISqModelUpdaterKey<UserName, TableUser>
        {
            public static UserNameUpdater Instance { get; } = new UserNameUpdater();
            IRecordSetterNext ISqModelUpdater<UserName, TableUser>.GetMapping(IDataMapSetter<TableUser, UserName> dataMapSetter)
            {
                return UserName.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<UserName, TableUser>.GetUpdateKeyMapping(IDataMapSetter<TableUser, UserName> dataMapSetter)
            {
                return UserName.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<UserName, TableUser>.GetUpdateMapping(IDataMapSetter<TableUser, UserName> dataMapSetter)
            {
                return UserName.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}