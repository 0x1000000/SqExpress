using System;
using System.Collections.Generic;

namespace SqExpress.CodeGenUtil.Model.SqModel
{
    internal class SqModelMeta
    {
        private readonly List<SqModelPropertyMeta> _properties = new List<SqModelPropertyMeta>();

        public SqModelMeta(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<SqModelPropertyMeta> Properties => this._properties;

        public SqModelPropertyMeta AddPropertyCheckExistence(SqModelPropertyMeta candidate)
        {
            var result = this._properties.Find(p => p.Name == candidate.Name);
            if (result != null)
            {
                if (result.Type != candidate.Type || result.CastType != candidate.CastType)
                {
                    throw new SqExpressCodeGenException($"Property \"{this.Name}.{candidate.Name}\" was declared several times with different types.");
                }

                return result;
            }
            this._properties.Add(candidate);
            return candidate;
        }
    }

    internal class SqModelPropertyMeta
    {
        private readonly List<SqModelPropertyTableColMeta> _column = new List<SqModelPropertyTableColMeta>();

        public SqModelPropertyMeta(string name, string type, string? castType, bool isPrimaryKey, bool isIdentity)
        {
            this.Name = name;
            this.Type = type;
            this.CastType = castType;
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;
        }

        public string Name { get; }

        public string Type { get; }

        public string? CastType { get; }

        public string FinalType => this.CastType ?? this.Type;

        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public IReadOnlyList<SqModelPropertyTableColMeta> Column => this._column;

        public void AddColumnCheckExistence(string modelName, SqModelPropertyTableColMeta candidate)
        {
            foreach (var c in this._column)
            {
                if (c.TableRef.Equals(candidate.TableRef))
                {
                    throw new SqExpressCodeGenException($"Property \"{modelName}.{this.Name}\" was declared several times in one table descriptor.");
                }
            }
            this._column.Add(candidate);
        }
    }

    public readonly struct SqModelTableRef : IEquatable<SqModelTableRef>
    {
        public string TableTypeName { get; }

        public string TableTypeNameSpace { get; }

        public SqModelTableRef(string tableTypeName, string tableTypeNameSpace)
        {
            this.TableTypeName = tableTypeName;
            this.TableTypeNameSpace = tableTypeNameSpace;
        }

        public bool Equals(SqModelTableRef other)
        {
            return this.TableTypeName == other.TableTypeName && this.TableTypeNameSpace == other.TableTypeNameSpace;
        }

        public override bool Equals(object? obj)
        {
            return obj is SqModelTableRef other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.TableTypeName, this.TableTypeNameSpace);
        }
    }

    public class SqModelPropertyTableColMeta
    {
        public SqModelPropertyTableColMeta(SqModelTableRef tableRef, string columnName)
        {
            this.TableRef = tableRef;
            this.ColumnName = columnName;
        }

        public SqModelTableRef TableRef { get; }

        public string ColumnName { get; }
    }
}