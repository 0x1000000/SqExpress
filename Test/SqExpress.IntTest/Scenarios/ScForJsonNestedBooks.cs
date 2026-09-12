using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScForJsonNestedBooks : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var shelve = new ShelveTable();
        var book = new BookTable();

        await context.Database.Statement(shelve.Script.Create());
        await context.Database.Statement(book.Script.Create());
        try
        {
            var shelves = new[]
            {
                new ShelveRow(1, "Aisle 3, Tier 1"),
                new ShelveRow(2, "Aisle 3, Tier 2"),
                new ShelveRow(3, "Aisle 5, Tier 4"),
                new ShelveRow(4, "Fiction Section, Row A")
            };
            await InsertDataInto(shelve, shelves)
                .MapData(m => m.Set(m.Target.ShelveId, m.Source.Id).Set(m.Target.Position, m.Source.Position))
                .Exec(context.Database);

            var books = new[]
            {
                new BookRow(101, 1, "The Hobbit", "J.R.R. Tolkien"),
                new BookRow(102, 1, "The Fellowship of the Ring", "J.R.R. Tolkien"),
                new BookRow(103, 2, "1984", "George Orwell"),
                new BookRow(104, 3, "A Brief History of Time", "Stephen Hawking"),
                new BookRow(105, 4, "To Kill a Mockingbird", "Harper Lee"),
                new BookRow(106, null, "Unassigned Draft Book", "Unknown Author")
            };
            await InsertDataInto(book, books)
                .MapData(m => m
                    .Set(m.Target.BookId, m.Source.Id)
                    .Set(m.Target.ShelveId, m.Source.ShelveId)
                    .Set(m.Target.Title, m.Source.Title)
                    .Set(m.Target.Author, m.Source.Author)
                )
                .Exec(context.Database);

            var booksJson = Select(
                    book.BookId.AsJson("$.id"),
                    book.Title.AsJson("$.bookdata.title"),
                    book.Author.AsJson("$.bookdata.author")
                )
                .From(book)
                .Where(book.ShelveId == shelve.ShelveId)
                .ForJson();

            var result = Select(
                    shelve.ShelveId,
                    shelve.Position,
                    ValueQuery(booksJson).AsJson("$.book")
                )
                .From(shelve)
                .ForJson();

            var value = await result.QueryScalar(context.Database);
            using var json = JsonDocument.Parse(Convert.ToString(value)!);
            var root = json.RootElement;
            if (root.GetArrayLength() != 4) throw new Exception("Expected four shelves in nested FOR JSON output.");

            var first = root.EnumerateArray().Single(i => i.GetProperty("ShelveId").GetInt32() == 1);
            var firstBooks = first.GetProperty("book");
            if (firstBooks.ValueKind != JsonValueKind.Array || firstBooks.GetArrayLength() != 2)
                throw new Exception("Shelf 1 must contain two embedded book objects.");
            var hobbit = firstBooks.EnumerateArray().Single(i => i.GetProperty("id").GetInt32() == 101);
            if (hobbit.GetProperty("bookdata").GetProperty("title").GetString() != "The Hobbit" ||
                hobbit.GetProperty("bookdata").GetProperty("author").GetString() != "J.R.R. Tolkien")
                throw new Exception("Nested bookdata JSON has unexpected content.");
        }
        finally
        {
            await context.Database.Statement(book.Script.DropIfExist());
            await context.Database.Statement(shelve.Script.DropIfExist());
        }
    }

    private sealed record ShelveRow(int Id, string Position);

    private sealed record BookRow(int Id, int? ShelveId, string Title, string Author);

    private sealed class ShelveTable : TempTableBase
    {
        public ShelveTable(Alias alias = default) : base("JsonShelve", alias)
        {
            this.ShelveId = this.CreateInt32Column("ShelveId", ColumnMeta.PrimaryKey());
            this.Position = this.CreateStringColumn("Position", 100, true);
        }

        public Int32TableColumn ShelveId { get; }

        public StringTableColumn Position { get; }
    }

    private sealed class BookTable : TempTableBase
    {
        public BookTable(Alias alias = default) : base("JsonBook", alias)
        {
            this.BookId = this.CreateInt32Column("BookId", ColumnMeta.PrimaryKey());
            this.ShelveId = this.CreateNullableInt32Column("ShelveId");
            this.Title = this.CreateStringColumn("Title", 100, true);
            this.Author = this.CreateStringColumn("Author", 100, true);
        }

        public Int32TableColumn BookId { get; }

        public NullableInt32TableColumn ShelveId { get; }

        public StringTableColumn Title { get; }

        public StringTableColumn Author { get; }
    }
}
