using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Ef
{
    internal sealed class EfMetadataDocument
    {
        public string ProviderName { get; set; } = "";

        public List<EfTableMetadata> Tables { get; set; } = new List<EfTableMetadata>();
    }

    internal sealed class EfTableMetadata
    {
        public string Schema { get; set; } = "";

        public string Name { get; set; } = "";

        public List<EfColumnMetadata> Columns { get; set; } = new List<EfColumnMetadata>();

        public List<EfIndexMetadata> Indexes { get; set; } = new List<EfIndexMetadata>();
    }

    internal sealed class EfColumnMetadata
    {
        public string Name { get; set; } = "";

        public string StoreType { get; set; } = "";

        public string ClrType { get; set; } = "";

        public bool Nullable { get; set; }

        public int? MaxLength { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public bool? Unicode { get; set; }

        public bool Identity { get; set; }

        public int? PrimaryKeyIndex { get; set; }

        public string? DefaultValueKind { get; set; }

        public string? DefaultValue { get; set; }

        public List<EfColumnRefMetadata> ForeignKeys { get; set; } = new List<EfColumnRefMetadata>();
    }

    internal sealed class EfColumnRefMetadata
    {
        public string Schema { get; set; } = "";

        public string Table { get; set; } = "";

        public string Column { get; set; } = "";
    }

    internal sealed class EfIndexMetadata
    {
        public string Name { get; set; } = "";

        public bool Unique { get; set; }

        public bool Clustered { get; set; }

        public List<EfIndexColumnMetadata> Columns { get; set; } = new List<EfIndexColumnMetadata>();
    }

    internal sealed class EfIndexColumnMetadata
    {
        public string Name { get; set; } = "";

        public bool Descending { get; set; }
    }
}
