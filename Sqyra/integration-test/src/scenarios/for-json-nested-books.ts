import {
  column,
  defineTable,
  insertInto,
  jsonOutput,
  nullableColumn,
  select,
  selectJson,
  sqlType,
  type InlineExportOptions,
} from "sqyra";
import { scalar } from "../context.js";
import type { Scenario } from "./types.js";

export const forJsonNestedBooksScenario: Scenario = {
  source: "ScForJsonNestedBooks",
  async run(context) {
    const shelves = defineTable({
      schema: null,
      name: "JsonShelve",
      temporary: true,
      columns: {
        ShelveId: column(sqlType.int32, { primaryKey: true }),
        Position: column(sqlType.string(100, { unicode: true })),
      },
    });
    const books = defineTable({
      schema: null,
      name: "JsonBook",
      temporary: true,
      columns: {
        BookId: column(sqlType.int32, { primaryKey: true }),
        ShelveId: nullableColumn(sqlType.int32),
        Title: column(sqlType.string(100, { unicode: true })),
        Author: column(sqlType.string(100, { unicode: true })),
      },
    });
    const script: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(books.$script.dropIfExists().toSql(script));
    await context.database.executeScript(shelves.$script.dropIfExists().toSql(script));
    await context.database.executeScript(shelves.$script.create().toSql(script));
    await context.database.executeScript(books.$script.create().toSql(script));
    try {
      const shelfRows = [
        { ShelveId: 1, Position: "Aisle 3, Tier 1" },
        { ShelveId: 2, Position: "Aisle 3, Tier 2" },
        { ShelveId: 3, Position: "Aisle 5, Tier 4" },
        { ShelveId: 4, Position: "Fiction Section, Row A" },
      ];
      await context.execute(
        insertInto(shelves)
          .columns("ShelveId", "Position")
          .values(shelfRows[0]!, ...shelfRows.slice(1)),
      );
      const bookRows = [
        { BookId: 101, ShelveId: 1, Title: "The Hobbit", Author: "J.R.R. Tolkien" },
        { BookId: 102, ShelveId: 1, Title: "The Fellowship of the Ring", Author: "J.R.R. Tolkien" },
        { BookId: 103, ShelveId: 2, Title: "1984", Author: "George Orwell" },
        { BookId: 104, ShelveId: 3, Title: "A Brief History of Time", Author: "Stephen Hawking" },
        { BookId: 105, ShelveId: 4, Title: "To Kill a Mockingbird", Author: "Harper Lee" },
        { BookId: 106, ShelveId: null, Title: "Unassigned Draft Book", Author: "Unknown Author" },
      ];
      await context.execute(
        insertInto(books)
          .columns("BookId", "ShelveId", "Title", "Author")
          .values(bookRows[0]!, ...bookRows.slice(1)),
      );
      const booksSql = selectJson(
        jsonOutput(books.BookId, "$.id"),
        jsonOutput(books.Title, "$.bookdata.title"),
        jsonOutput(books.Author, "$.bookdata.author"),
      )
        .from(books)
        .where(books.ShelveId.eq(shelves.ShelveId));
      const shelfSql = select(
        shelves.ShelveId,
        shelves.Position,
        jsonOutput(booksSql.forJson().scalarSubquery(), "$.book"),
      ).from(shelves);
      const value = scalar(await context.query(shelfSql.forJson()));
      if (typeof value !== "string")
        throw new Error("Nested FOR JSON returned SQL NULL or a non-string value.");
      const parsed: unknown = JSON.parse(value);
      if (!Array.isArray(parsed) || parsed.length !== 4)
        throw new Error("Expected four shelves in nested FOR JSON output.");
      const firstMatches = parsed.filter(
        (item: unknown) =>
          typeof item === "object" && item !== null && "ShelveId" in item && item.ShelveId === 1,
      );
      if (firstMatches.length !== 1)
        throw new Error("C# Single requires exactly one shelf with ShelveId 1.");
      const first: unknown = firstMatches[0];
      if (
        typeof first !== "object" ||
        first === null ||
        !("book" in first) ||
        !Array.isArray(first.book) ||
        first.book.length !== 2
      )
        throw new Error("Shelf 1 must contain two embedded book objects.");
      const hobbitMatches = first.book.filter(
        (item: unknown) =>
          typeof item === "object" && item !== null && "id" in item && item.id === 101,
      );
      if (hobbitMatches.length !== 1)
        throw new Error("C# Single requires exactly one book with id 101.");
      const hobbit: unknown = hobbitMatches[0];
      if (
        typeof hobbit !== "object" ||
        hobbit === null ||
        !("bookdata" in hobbit) ||
        typeof hobbit.bookdata !== "object" ||
        hobbit.bookdata === null ||
        !("title" in hobbit.bookdata) ||
        !("author" in hobbit.bookdata) ||
        hobbit.bookdata.title !== "The Hobbit" ||
        hobbit.bookdata.author !== "J.R.R. Tolkien"
      )
        throw new Error("Nested bookdata JSON has unexpected content.");
    } finally {
      await context.database.executeScript(books.$script.dropIfExists().toSql(script));
      await context.database.executeScript(shelves.$script.dropIfExists().toSql(script));
    }
  },
};
