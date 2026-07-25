# Table Descriptor Generation

SqExpress supports two independent table-generation workflows. Choose the guide that matches the source of truth for your schema:

1. [Database-First Table Generation](database_first_table_generation.md)  
   Generate descriptors by connecting directly to Microsoft SQL Server, MySQL/MariaDB, or PostgreSQL.

2. [Entity Framework Table Generation](ef_table_generation.md)  
   Generate descriptors automatically from an EF Core SQL Server model during `dotnet build`.

For new projects, attribute-based table declarations are recommended in both workflows. See the [Table Description Reference](table_description.md) for the available declaration attributes.
