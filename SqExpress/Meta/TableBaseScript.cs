﻿using SqExpress.StatementSyntax;

namespace SqExpress.Meta
{
    public readonly struct TableBaseScript
    {
        private readonly TableBase _table;

        public TableBaseScript(TableBase table)
        {
            this._table = table;
        }

        public IStatement DropAndCreate()
        {
            return StatementList.Combine(this.DropIfExist(), this.Create());
        }

        public IStatement DropIfExist()
        {
            return new StatementDropTable(this._table, ifExists: true);
        }

        public IStatement Create()
        {
            return new StatementCreateTable(this._table);
        }
    }
}