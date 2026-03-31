## Release 0.0.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
SQEX001 | Migration | Info | `SqTSqlParser.Parse/TryParse` call with compile-time SQL can be converted to SqExpress code.
SQEX010 | Correctness | Error | `SqTSqlParser.Parse` SQL text cannot be parsed by `SqTSqlParser`.
SQEX011 | Correctness | Warning | Referenced SQL tables cannot be resolved to discovered SqExpress table classes.
SQEX012 | Correctness | Warning | Referenced SQL columns cannot be resolved to discovered SqExpress table members.
SQEX100 | SourceGeneration | Error | `[TableDescriptor]` target must be a class.
SQEX101 | SourceGeneration | Error | `[TableDescriptor]` class must be partial.
SQEX102 | SourceGeneration | Error | `[TableDescriptor]` class must be top-level.
SQEX103 | SourceGeneration | Error | `[TableDescriptor]` class must be non-generic.
SQEX104 | SourceGeneration | Error | `[TableDescriptor]` class must not specify a custom base type.
SQEX105 | SourceGeneration | Error | `[TableDescriptor]` declaration is invalid.
SQEX106 | SourceGeneration | Error | Multiple generated descriptor classes target the same SQL table.
SQEX107 | SourceGeneration | Error | Duplicate SQL columns were declared on the generated descriptor.
SQEX108 | SourceGeneration | Error | Generated or explicit table descriptor property name is invalid.
SQEX109 | SourceGeneration | Error | Generated table descriptor property names collide.
SQEX110 | SourceGeneration | Error | An index references an unknown SQL column.
SQEX111 | SourceGeneration | Error | A descending index column is not part of the index definition.
SQEX112 | SourceGeneration | Error | Foreign key target table could not be resolved.
SQEX113 | SourceGeneration | Error | Foreign key target column could not be resolved.
SQEX114 | SourceGeneration | Error | A table descriptor default value cannot be parsed for the declared column type.
SQEX115 | SourceGeneration | Error | `[TableDescriptor]` and `[TempTableDescriptor]` cannot be used on the same class.
SQEX116 | SourceGeneration | Error | Class-level `SqModel` name is invalid.
SQEX117 | SourceGeneration | Error | A column-level `SqModels` entry is malformed.
SQEX118 | SourceGeneration | Error | A generated or explicit SqModel property name is invalid.
SQEX119 | SourceGeneration | Error | The same SqModel property was declared with conflicting CLR types or casts.
SQEX120 | SourceGeneration | Error | The same SqModel property was mapped from multiple columns in one table declaration.
SQEX121 | SourceGeneration | Error | The same SqModel has inconsistent property sets across multiple table declarations.
