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
