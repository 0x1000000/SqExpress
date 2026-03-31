using SqExpress.TableDecalationAttributes;

namespace SqExpress.GetStarted.FavoriteFilters;

[TableDescriptor("dbo", "FavoriteFilter")]
[Int32Column("FavoriteFilterId", Pk = true, Identity = true)]
[StringColumn("Name", MaxLength = 255)]
public partial class TableFavoriteFilter;

[TableDescriptor("dbo", "FavoriteFilterItem")]
[Int32Column("FavoriteFilterId", FkTable = "FavoriteFilter", FkColumn = "FavoriteFilterId")]
[Int32Column("Id")]
[Int32Column("ParentId")]
[NullableInt32Column("ArrayIndex")]
[BooleanColumn("IsTypeTag")]
[StringColumn("Tag", MaxLength = 255)]
[NullableStringColumn("Value", Unicode = true, Text = true)]
public partial class TableFavoriteFilterItem;
