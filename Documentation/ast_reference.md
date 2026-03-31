# AST Reference

This document is generated from the current `IExpr` hierarchy in the SqExpress syntax tree.

<!-- CodeGenStart -->
## Hierarchy

- [IExpr](#iexpr)
  - [ExprBoolean](#exprboolean) _(abstract)_
    - [ExprBooleanAnd](#exprbooleanand)
    - [ExprBooleanNot](#exprbooleannot)
    - [ExprBooleanOr](#exprbooleanor)
    - [ExprPredicate](#exprpredicate) _(abstract)_
      - [ExprExists](#exprexists)
      - [ExprIn](#exprin) _(abstract)_
        - [ExprInSubQuery](#exprinsubquery)
        - [ExprInValues](#exprinvalues)
      - [ExprIsNull](#exprisnull)
      - [ExprLike](#exprlike)
      - [ExprPredicateLeftRight](#exprpredicateleftright) _(abstract)_
        - [ExprBooleanEq](#exprbooleaneq)
        - [ExprBooleanGt](#exprbooleangt)
        - [ExprBooleanGtEq](#exprbooleangteq)
        - [ExprBooleanLt](#exprbooleanlt)
        - [ExprBooleanLtEq](#exprbooleanlteq)
        - [ExprBooleanNotEq](#exprbooleannoteq)
  - [ExprColumnSetClause](#exprcolumnsetclause)
  - [ExprDbSchema](#exprdbschema)
  - [ExprFrameBorder](#exprframeborder) _(abstract)_
    - [ExprCurrentRowFrameBorder](#exprcurrentrowframeborder) _(singleton)_
    - [ExprUnboundedFrameBorder](#exprunboundedframeborder)
    - [ExprValueFrameBorder](#exprvalueframeborder)
  - [ExprFrameClause](#exprframeclause)
  - [ExprInsertValueRow](#exprinsertvaluerow)
  - [ExprOffsetFetch](#exproffsetfetch)
  - [ExprOrderBy](#exprorderby)
  - [ExprOrderByItem](#exprorderbyitem)
  - [ExprOrderByOffsetFetch](#exprorderbyoffsetfetch)
  - [ExprOutput](#exproutput)
  - [ExprOver](#exprover)
  - [ExprTableValueConstructor](#exprtablevalueconstructor)
  - [ExprType](#exprtype) _(abstract)_
    - [ExprTypeBoolean](#exprtypeboolean) _(singleton)_
    - [ExprTypeByte](#exprtypebyte) _(singleton)_
    - [ExprTypeByteArrayBase](#exprtypebytearraybase) _(abstract)_
      - [ExprTypeByteArray](#exprtypebytearray)
      - [ExprTypeFixSizeByteArray](#exprtypefixsizebytearray)
    - [ExprTypeDateTime](#exprtypedatetime)
    - [ExprTypeDateTimeOffset](#exprtypedatetimeoffset) _(singleton)_
    - [ExprTypeDecimal](#exprtypedecimal)
    - [ExprTypeDouble](#exprtypedouble) _(singleton)_
    - [ExprTypeGuid](#exprtypeguid) _(singleton)_
    - [ExprTypeInt16](#exprtypeint16) _(singleton)_
    - [ExprTypeInt32](#exprtypeint32) _(singleton)_
    - [ExprTypeInt64](#exprtypeint64) _(singleton)_
    - [ExprTypeStringBase](#exprtypestringbase) _(abstract)_
      - [ExprTypeFixSizeString](#exprtypefixsizestring)
      - [ExprTypeString](#exprtypestring)
      - [ExprTypeXml](#exprtypexml) _(singleton)_
  - [ExprValueRow](#exprvaluerow)
  - [IExprAlias](#iexpralias) _(interface)_
    - [ExprAlias](#expralias)
    - [ExprAliasGuid](#expraliasguid)
  - [IExprAssigning](#iexprassigning) _(interface)_
    - [ExprDefault](#exprdefault) _(singleton)_
  - [IExprColumnSource](#iexprcolumnsource) _(interface)_
    - [ExprTableAlias](#exprtablealias)
    - [IExprTableFullName](#iexprtablefullname) _(interface)_
      - [ExprTableFullName](#exprtablefullname)
  - [IExprComplete](#iexprcomplete) _(interface)_
    - [IExprExec](#iexprexec) _(interface)_
      - [ExprDelete](#exprdelete)
      - [ExprIdentityInsert](#expridentityinsert)
      - [ExprInsert](#exprinsert)
      - [ExprList](#exprlist)
      - [ExprMerge](#exprmerge)
        - [ExprMergeOutput](#exprmergeoutput)
      - [ExprUpdate](#exprupdate)
    - [IExprQuery](#iexprquery) _(interface)_
      - [ExprDeleteOutput](#exprdeleteoutput)
      - [ExprInsertOutput](#exprinsertoutput)
      - [ExprQueryList](#exprquerylist)
      - [IExprReadOnlyQuery](#iexprreadonlyquery) _(interface)_
        - [ExprSelect](#exprselect)
        - [IExprSubQuery](#iexprsubquery) _(interface)_
          - [ExprSelectOffsetFetch](#exprselectoffsetfetch)
          - [IExprQueryExpression](#iexprqueryexpression) _(interface)_
            - [ExprQueryExpression](#exprqueryexpression)
            - [ExprQuerySpecification](#exprqueryspecification)
  - [IExprInsertSource](#iexprinsertsource) _(interface)_
    - [ExprInsertQuery](#exprinsertquery)
    - [ExprInsertValues](#exprinsertvalues)
  - [IExprMergeMatched](#iexprmergematched) _(interface)_
    - [ExprMergeMatchedDelete](#exprmergematcheddelete)
    - [ExprMergeMatchedUpdate](#exprmergematchedupdate)
  - [IExprMergeNotMatched](#iexprmergenotmatched) _(interface)_
    - [ExprExprMergeNotMatchedInsert](#exprexprmergenotmatchedinsert)
    - [ExprExprMergeNotMatchedInsertDefault](#exprexprmergenotmatchedinsertdefault)
  - [IExprName](#iexprname) _(interface)_
    - [ExprColumnAlias](#exprcolumnalias)
    - [ExprColumnName](#exprcolumnname)
    - [ExprDatabaseName](#exprdatabasename)
    - [ExprFunctionName](#exprfunctionname)
    - [ExprSchemaName](#exprschemaname)
    - [ExprTableName](#exprtablename)
    - [ExprTempTableName](#exprtemptablename)
  - [IExprOutputColumn](#iexproutputcolumn) _(interface)_
    - [ExprOutputAction](#exproutputaction)
    - [ExprOutputColumn](#exproutputcolumn)
    - [ExprOutputColumnDeleted](#exproutputcolumndeleted)
    - [ExprOutputColumnInserted](#exproutputcolumninserted)
  - [IExprSelecting](#iexprselecting) _(interface)_
    - [ExprAggregateFunction](#expraggregatefunction)
    - [ExprAggregateOverFunction](#expraggregateoverfunction)
    - [ExprAllColumns](#exprallcolumns)
    - [ExprAnalyticFunction](#expranalyticfunction)
    - [ExprSelecting](#exprselecting) _(abstract)_
      - [ExprValue](#exprvalue) _(abstract)_
        - [ExprArithmetic](#exprarithmetic) _(abstract)_
          - [ExprDiv](#exprdiv)
          - [ExprModulo](#exprmodulo)
          - [ExprMul](#exprmul)
          - [ExprSub](#exprsub)
          - [ExprSum](#exprsum)
        - [ExprBitwise](#exprbitwise) _(abstract)_
          - [ExprBitwiseAnd](#exprbitwiseand)
          - [ExprBitwiseNot](#exprbitwisenot)
          - [ExprBitwiseOr](#exprbitwiseor)
          - [ExprBitwiseXor](#exprbitwisexor)
        - [ExprCase](#exprcase)
        - [ExprCaseWhenThen](#exprcasewhenthen)
        - [ExprCast](#exprcast)
        - [ExprColumn](#exprcolumn)
        - [ExprDateAdd](#exprdateadd)
        - [ExprDateDiff](#exprdatediff)
        - [ExprFuncCoalesce](#exprfunccoalesce)
        - [ExprFuncIsNull](#exprfuncisnull)
        - [ExprGetDate](#exprgetdate) _(singleton)_
        - [ExprGetUtcDate](#exprgetutcdate) _(singleton)_
        - [ExprLiteral](#exprliteral) _(abstract)_
          - [ExprBoolLiteral](#exprboolliteral)
          - [ExprByteArrayLiteral](#exprbytearrayliteral)
          - [ExprByteLiteral](#exprbyteliteral)
          - [ExprDateTimeLiteral](#exprdatetimeliteral)
          - [ExprDateTimeOffsetLiteral](#exprdatetimeoffsetliteral)
          - [ExprDecimalLiteral](#exprdecimalliteral)
          - [ExprDoubleLiteral](#exprdoubleliteral)
          - [ExprGuidLiteral](#exprguidliteral)
          - [ExprInt16Literal](#exprint16literal)
          - [ExprInt32Literal](#exprint32literal)
          - [ExprInt64Literal](#exprint64literal)
          - [ExprStringLiteral](#exprstringliteral)
        - [ExprNull](#exprnull) _(singleton)_
        - [ExprParameter](#exprparameter)
        - [ExprPortableScalarFunction](#exprportablescalarfunction)
        - [ExprScalarFunction](#exprscalarfunction)
        - [ExprSelectingValue](#exprselectingvalue)
        - [ExprStringConcat](#exprstringconcat)
        - [ExprUnsafeValue](#exprunsafevalue)
        - [ExprValueQuery](#exprvaluequery)
    - [IExprNamedSelecting](#iexprnamedselecting) _(interface)_
      - [ExprAliasedColumn](#expraliasedcolumn)
      - [ExprAliasedColumnName](#expraliasedcolumnname)
      - [ExprAliasedSelecting](#expraliasedselecting)
  - [IExprSelectingSource](#iexprselectingsource) _(interface)_
    - [ISubQuerySource](#isubquerysource) _(interface)_
      - [IExprTableSource](#iexprtablesource) _(interface)_
        - [ExprAliasedTableFunction](#expraliasedtablefunction)
        - [ExprCrossedTable](#exprcrossedtable)
        - [ExprCte](#exprcte) _(abstract)_
          - [ExprCteQuery](#exprctequery)
        - [ExprDerivedTable](#exprderivedtable) _(abstract)_
          - [ExprDerivedTableQuery](#exprderivedtablequery)
          - [ExprDerivedTableValues](#exprderivedtablevalues)
        - [ExprJoinedTable](#exprjoinedtable)
        - [ExprLateralCrossedTable](#exprlateralcrossedtable)
        - [ExprTable](#exprtable)
        - [ExprTableFunction](#exprtablefunction)

## Type Reference

### IExpr

- Kind: interface root
- Direct descendants: [ExprBoolean](#exprboolean), [ExprColumnSetClause](#exprcolumnsetclause), [ExprDbSchema](#exprdbschema), [ExprFrameBorder](#exprframeborder), [ExprFrameClause](#exprframeclause), [ExprInsertValueRow](#exprinsertvaluerow), [ExprOffsetFetch](#exproffsetfetch), [ExprOrderBy](#exprorderby), [ExprOrderByItem](#exprorderbyitem), [ExprOrderByOffsetFetch](#exprorderbyoffsetfetch), [ExprOutput](#exproutput), [ExprOver](#exprover), [ExprTableValueConstructor](#exprtablevalueconstructor), [ExprType](#exprtype), [ExprValueRow](#exprvaluerow), [IExprAlias](#iexpralias), [IExprAssigning](#iexprassigning), [IExprColumnSource](#iexprcolumnsource), [IExprComplete](#iexprcomplete), [IExprInsertSource](#iexprinsertsource), [IExprMergeMatched](#iexprmergematched), [IExprMergeNotMatched](#iexprmergenotmatched), [IExprName](#iexprname), [IExprOutputColumn](#iexproutputcolumn), [IExprSelecting](#iexprselecting), [IExprSelectingSource](#iexprselectingsource)

### ExprAggregateFunction

- Kind: class
- Base: [IExprSelecting](#iexprselecting)
- Subnodes:
  - `Expression`: [ExprValue](#exprvalue)
  - `Name`: [ExprFunctionName](#exprfunctionname)
- Plain properties:
  - `IsDistinct`: Boolean

### ExprAggregateOverFunction

- Kind: class
- Base: [IExprSelecting](#iexprselecting)
- Subnodes:
  - `Function`: [ExprAggregateFunction](#expraggregatefunction)
  - `Over`: [ExprOver](#exprover)

### ExprAlias

- Kind: class
- Base: [IExprAlias](#iexpralias)
- Plain properties:
  - `Name`: String

### ExprAliasGuid

- Kind: class
- Base: [IExprAlias](#iexpralias)
- Plain properties:
  - `Id`: Guid

### ExprAliasedColumn

- Kind: class
- Base: [IExprNamedSelecting](#iexprnamedselecting)
- Subnodes:
  - `Alias`: [ExprColumnAlias](#exprcolumnalias)?
  - `Column`: [ExprColumn](#exprcolumn)

### ExprAliasedColumnName

- Kind: class
- Base: [IExprNamedSelecting](#iexprnamedselecting)
- Subnodes:
  - `Alias`: [ExprColumnAlias](#exprcolumnalias)?
  - `Column`: [ExprColumnName](#exprcolumnname)

### ExprAliasedSelecting

- Kind: class
- Base: [IExprNamedSelecting](#iexprnamedselecting)
- Subnodes:
  - `Alias`: [ExprColumnAlias](#exprcolumnalias)
  - `Value`: [IExprSelecting](#iexprselecting)

### ExprAliasedTableFunction

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)
  - `Function`: [ExprTableFunction](#exprtablefunction)

### ExprAllColumns

- Kind: class
- Base: [IExprSelecting](#iexprselecting)
- Subnodes:
  - `Source`: [IExprColumnSource](#iexprcolumnsource)?

### ExprAnalyticFunction

- Kind: class
- Base: [IExprSelecting](#iexprselecting)
- Subnodes:
  - `Arguments`: IReadOnlyList<[ExprValue](#exprvalue)>?
  - `Name`: [ExprFunctionName](#exprfunctionname)
  - `Over`: [ExprOver](#exprover)

### ExprArithmetic

- Kind: abstract class
- Base: [ExprValue](#exprvalue)
- Direct descendants: [ExprDiv](#exprdiv), [ExprModulo](#exprmodulo), [ExprMul](#exprmul), [ExprSub](#exprsub), [ExprSum](#exprsum)

### ExprBitwise

- Kind: abstract class
- Base: [ExprValue](#exprvalue)
- Direct descendants: [ExprBitwiseAnd](#exprbitwiseand), [ExprBitwiseNot](#exprbitwisenot), [ExprBitwiseOr](#exprbitwiseor), [ExprBitwiseXor](#exprbitwisexor)

### ExprBitwiseAnd

- Kind: class
- Base: [ExprBitwise](#exprbitwise)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBitwiseNot

- Kind: class
- Base: [ExprBitwise](#exprbitwise)
- Subnodes:
  - `Value`: [ExprValue](#exprvalue)

### ExprBitwiseOr

- Kind: class
- Base: [ExprBitwise](#exprbitwise)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBitwiseXor

- Kind: class
- Base: [ExprBitwise](#exprbitwise)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBoolLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Boolean?

### ExprBoolean

- Kind: abstract class
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprBooleanAnd](#exprbooleanand), [ExprBooleanNot](#exprbooleannot), [ExprBooleanOr](#exprbooleanor), [ExprPredicate](#exprpredicate)

### ExprBooleanAnd

- Kind: class
- Base: [ExprBoolean](#exprboolean)
- Subnodes:
  - `Left`: [ExprBoolean](#exprboolean)
  - `Right`: [ExprBoolean](#exprboolean)

### ExprBooleanEq

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanGt

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanGtEq

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanLt

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanLtEq

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanNot

- Kind: class
- Base: [ExprBoolean](#exprboolean)
- Subnodes:
  - `Expr`: [ExprBoolean](#exprboolean)

### ExprBooleanNotEq

- Kind: class
- Base: [ExprPredicateLeftRight](#exprpredicateleftright)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprBooleanOr

- Kind: class
- Base: [ExprBoolean](#exprboolean)
- Subnodes:
  - `Left`: [ExprBoolean](#exprboolean)
  - `Right`: [ExprBoolean](#exprboolean)

### ExprByteArrayLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: IReadOnlyList<Byte>?

### ExprByteLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Byte?

### ExprCase

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Cases`: IReadOnlyList<[ExprCaseWhenThen](#exprcasewhenthen)>
  - `DefaultValue`: [ExprValue](#exprvalue)

### ExprCaseWhenThen

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Condition`: [ExprBoolean](#exprboolean)
  - `Value`: [ExprValue](#exprvalue)

### ExprCast

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Expression`: [IExprSelecting](#iexprselecting)
  - `SqlType`: [ExprType](#exprtype)

### ExprColumn

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `ColumnName`: [ExprColumnName](#exprcolumnname)
  - `Source`: [IExprColumnSource](#iexprcolumnsource)?

### ExprColumnAlias

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprColumnName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprColumnSetClause

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Column`: [ExprColumn](#exprcolumn)
  - `Value`: [IExprAssigning](#iexprassigning)

### ExprCrossedTable

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Left`: [IExprTableSource](#iexprtablesource)
  - `Right`: [IExprTableSource](#iexprtablesource)

### ExprCte

- Kind: abstract class
- Base: [IExprTableSource](#iexprtablesource)
- Direct descendants: [ExprCteQuery](#exprctequery)
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)?
- Plain properties:
  - `Name`: String

### ExprCteQuery

- Kind: class
- Base: [ExprCte](#exprcte)
- Traversal: custom
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)?
  - `Query`: [IExprSubQuery](#iexprsubquery)
- Plain properties:
  - `Name`: String

### ExprCurrentRowFrameBorder

- Kind: class, singleton
- Base: [ExprFrameBorder](#exprframeborder)

### ExprDatabaseName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprDateAdd

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Date`: [ExprValue](#exprvalue)
- Plain properties:
  - `DatePart`: DateAddDatePart
  - `Number`: Int32

### ExprDateDiff

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `EndDate`: [ExprValue](#exprvalue)
  - `StartDate`: [ExprValue](#exprvalue)
- Plain properties:
  - `DatePart`: DateDiffDatePart

### ExprDateTimeLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: DateTime?

### ExprDateTimeOffsetLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: DateTimeOffset?

### ExprDbSchema

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Database`: [ExprDatabaseName](#exprdatabasename)?
  - `Schema`: [ExprSchemaName](#exprschemaname)

### ExprDecimalLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Decimal?

### ExprDefault

- Kind: class, singleton
- Base: [IExprAssigning](#iexprassigning)

### ExprDelete

- Kind: class
- Base: [IExprExec](#iexprexec)
- Subnodes:
  - `Filter`: [ExprBoolean](#exprboolean)?
  - `Source`: [IExprTableSource](#iexprtablesource)?
  - `Target`: [ExprTable](#exprtable)

### ExprDeleteOutput

- Kind: class
- Base: [IExprQuery](#iexprquery)
- Subnodes:
  - `Delete`: [ExprDelete](#exprdelete)
  - `OutputColumns`: IReadOnlyList<[ExprAliasedColumn](#expraliasedcolumn)>

### ExprDerivedTable

- Kind: abstract class
- Base: [IExprTableSource](#iexprtablesource)
- Direct descendants: [ExprDerivedTableQuery](#exprderivedtablequery), [ExprDerivedTableValues](#exprderivedtablevalues)
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)

### ExprDerivedTableQuery

- Kind: class
- Base: [ExprDerivedTable](#exprderivedtable)
- Traversal: custom
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)
  - `Columns`: IReadOnlyList<[ExprColumnName](#exprcolumnname)>?
  - `Query`: [IExprSubQuery](#iexprsubquery)

### ExprDerivedTableValues

- Kind: class
- Base: [ExprDerivedTable](#exprderivedtable)
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)
  - `Columns`: IReadOnlyList<[ExprColumnName](#exprcolumnname)>
  - `Values`: [ExprTableValueConstructor](#exprtablevalueconstructor)

### ExprDiv

- Kind: class
- Base: [ExprArithmetic](#exprarithmetic)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprDoubleLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Double?

### ExprExists

- Kind: class
- Base: [ExprPredicate](#exprpredicate)
- Subnodes:
  - `SubQuery`: [IExprSubQuery](#iexprsubquery)

### ExprExprMergeNotMatchedInsert

- Kind: class
- Base: [IExprMergeNotMatched](#iexprmergenotmatched)
- Subnodes:
  - `And`: [ExprBoolean](#exprboolean)?
  - `Columns`: IReadOnlyList<[ExprColumnName](#exprcolumnname)>
  - `Values`: IReadOnlyList<[IExprAssigning](#iexprassigning)>

### ExprExprMergeNotMatchedInsertDefault

- Kind: class
- Base: [IExprMergeNotMatched](#iexprmergenotmatched)
- Subnodes:
  - `And`: [ExprBoolean](#exprboolean)?

### ExprFrameBorder

- Kind: abstract class
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprCurrentRowFrameBorder](#exprcurrentrowframeborder), [ExprUnboundedFrameBorder](#exprunboundedframeborder), [ExprValueFrameBorder](#exprvalueframeborder)

### ExprFrameClause

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `End`: [ExprFrameBorder](#exprframeborder)?
  - `Start`: [ExprFrameBorder](#exprframeborder)

### ExprFuncCoalesce

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Alts`: IReadOnlyList<[ExprValue](#exprvalue)>
  - `Test`: [ExprValue](#exprvalue)

### ExprFuncIsNull

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Alt`: [ExprValue](#exprvalue)
  - `Test`: [ExprValue](#exprvalue)

### ExprFunctionName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `BuiltIn`: Boolean
  - `Name`: String

### ExprGetDate

- Kind: class, singleton
- Base: [ExprValue](#exprvalue)

### ExprGetUtcDate

- Kind: class, singleton
- Base: [ExprValue](#exprvalue)

### ExprGuidLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Guid?

### ExprIdentityInsert

- Kind: class
- Base: [IExprExec](#iexprexec)
- Subnodes:
  - `IdentityColumns`: IReadOnlyList<[ExprColumnName](#exprcolumnname)>
  - `Insert`: [ExprInsert](#exprinsert)

### ExprIn

- Kind: abstract class
- Base: [ExprPredicate](#exprpredicate)
- Direct descendants: [ExprInSubQuery](#exprinsubquery), [ExprInValues](#exprinvalues)
- Subnodes:
  - `TestExpression`: [ExprValue](#exprvalue)

### ExprInSubQuery

- Kind: class
- Base: [ExprIn](#exprin)
- Subnodes:
  - `SubQuery`: [IExprSubQuery](#iexprsubquery)
  - `TestExpression`: [ExprValue](#exprvalue)

### ExprInValues

- Kind: class
- Base: [ExprIn](#exprin)
- Subnodes:
  - `Items`: IReadOnlyList<[ExprValue](#exprvalue)>
  - `TestExpression`: [ExprValue](#exprvalue)

### ExprInsert

- Kind: class
- Base: [IExprExec](#iexprexec)
- Subnodes:
  - `Source`: [IExprInsertSource](#iexprinsertsource)
  - `Target`: [IExprTableFullName](#iexprtablefullname)
  - `TargetColumns`: IReadOnlyList<[ExprColumnName](#exprcolumnname)>?

### ExprInsertOutput

- Kind: class
- Base: [IExprQuery](#iexprquery)
- Subnodes:
  - `Insert`: [ExprInsert](#exprinsert)
  - `OutputColumns`: IReadOnlyList<[ExprAliasedColumnName](#expraliasedcolumnname)>

### ExprInsertQuery

- Kind: class
- Base: [IExprInsertSource](#iexprinsertsource)
- Subnodes:
  - `Query`: [IExprQuery](#iexprquery)

### ExprInsertValueRow

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Items`: IReadOnlyList<[IExprAssigning](#iexprassigning)>

### ExprInsertValues

- Kind: class
- Base: [IExprInsertSource](#iexprinsertsource)
- Subnodes:
  - `Items`: IReadOnlyList<[ExprInsertValueRow](#exprinsertvaluerow)>

### ExprInt16Literal

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Int16?

### ExprInt32Literal

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Int32?

### ExprInt64Literal

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: Int64?

### ExprIsNull

- Kind: class
- Base: [ExprPredicate](#exprpredicate)
- Subnodes:
  - `Test`: [ExprValue](#exprvalue)
- Plain properties:
  - `Not`: Boolean

### ExprJoinedTable

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Left`: [IExprTableSource](#iexprtablesource)
  - `Right`: [IExprTableSource](#iexprtablesource)
  - `SearchCondition`: [ExprBoolean](#exprboolean)
- Plain properties:
  - `JoinType`: ExprJoinedTable.ExprJoinType

### ExprLateralCrossedTable

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Left`: [IExprTableSource](#iexprtablesource)
  - `Right`: [IExprTableSource](#iexprtablesource)
- Plain properties:
  - `Outer`: Boolean

### ExprLike

- Kind: class
- Base: [ExprPredicate](#exprpredicate)
- Subnodes:
  - `Pattern`: [ExprValue](#exprvalue)
  - `Test`: [ExprValue](#exprvalue)

### ExprList

- Kind: class
- Base: [IExprExec](#iexprexec)
- Subnodes:
  - `Expressions`: IReadOnlyList<[IExprExec](#iexprexec)>

### ExprLiteral

- Kind: abstract class
- Base: [ExprValue](#exprvalue)
- Direct descendants: [ExprBoolLiteral](#exprboolliteral), [ExprByteArrayLiteral](#exprbytearrayliteral), [ExprByteLiteral](#exprbyteliteral), [ExprDateTimeLiteral](#exprdatetimeliteral), [ExprDateTimeOffsetLiteral](#exprdatetimeoffsetliteral), [ExprDecimalLiteral](#exprdecimalliteral), [ExprDoubleLiteral](#exprdoubleliteral), [ExprGuidLiteral](#exprguidliteral), [ExprInt16Literal](#exprint16literal), [ExprInt32Literal](#exprint32literal), [ExprInt64Literal](#exprint64literal), [ExprStringLiteral](#exprstringliteral)

### ExprMerge

- Kind: class
- Base: [IExprExec](#iexprexec)
- Direct descendants: [ExprMergeOutput](#exprmergeoutput)
- Subnodes:
  - `On`: [ExprBoolean](#exprboolean)
  - `Source`: [IExprTableSource](#iexprtablesource)
  - `TargetTable`: [ExprTable](#exprtable)
  - `WhenMatched`: [IExprMergeMatched](#iexprmergematched)?
  - `WhenNotMatchedBySource`: [IExprMergeMatched](#iexprmergematched)?
  - `WhenNotMatchedByTarget`: [IExprMergeNotMatched](#iexprmergenotmatched)?

### ExprMergeMatchedDelete

- Kind: class
- Base: [IExprMergeMatched](#iexprmergematched)
- Subnodes:
  - `And`: [ExprBoolean](#exprboolean)?

### ExprMergeMatchedUpdate

- Kind: class
- Base: [IExprMergeMatched](#iexprmergematched)
- Subnodes:
  - `And`: [ExprBoolean](#exprboolean)?
  - `Set`: IReadOnlyList<[ExprColumnSetClause](#exprcolumnsetclause)>

### ExprMergeOutput

- Kind: class
- Base: [ExprMerge](#exprmerge)
- Subnodes:
  - `On`: [ExprBoolean](#exprboolean)
  - `Output`: [ExprOutput](#exproutput)
  - `Source`: [IExprTableSource](#iexprtablesource)
  - `TargetTable`: [ExprTable](#exprtable)
  - `WhenMatched`: [IExprMergeMatched](#iexprmergematched)?
  - `WhenNotMatchedBySource`: [IExprMergeMatched](#iexprmergematched)?
  - `WhenNotMatchedByTarget`: [IExprMergeNotMatched](#iexprmergenotmatched)?

### ExprModulo

- Kind: class
- Base: [ExprArithmetic](#exprarithmetic)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprMul

- Kind: class
- Base: [ExprArithmetic](#exprarithmetic)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprNull

- Kind: class, singleton
- Base: [ExprValue](#exprvalue)

### ExprOffsetFetch

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Fetch`: [ExprValue](#exprvalue)?
  - `Offset`: [ExprValue](#exprvalue)

### ExprOrderBy

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `OrderList`: IReadOnlyList<[ExprOrderByItem](#exprorderbyitem)>

### ExprOrderByItem

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Value`: [ExprValue](#exprvalue)
- Plain properties:
  - `Descendant`: Boolean

### ExprOrderByOffsetFetch

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `OffsetFetch`: [ExprOffsetFetch](#exproffsetfetch)
  - `OrderList`: IReadOnlyList<[ExprOrderByItem](#exprorderbyitem)>

### ExprOutput

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Columns`: IReadOnlyList<[IExprOutputColumn](#iexproutputcolumn)>

### ExprOutputAction

- Kind: class
- Base: [IExprOutputColumn](#iexproutputcolumn)
- Subnodes:
  - `Alias`: [ExprColumnAlias](#exprcolumnalias)?

### ExprOutputColumn

- Kind: class
- Base: [IExprOutputColumn](#iexproutputcolumn)
- Subnodes:
  - `Column`: [ExprAliasedColumn](#expraliasedcolumn)

### ExprOutputColumnDeleted

- Kind: class
- Base: [IExprOutputColumn](#iexproutputcolumn)
- Subnodes:
  - `ColumnName`: [ExprAliasedColumnName](#expraliasedcolumnname)

### ExprOutputColumnInserted

- Kind: class
- Base: [IExprOutputColumn](#iexproutputcolumn)
- Subnodes:
  - `ColumnName`: [ExprAliasedColumnName](#expraliasedcolumnname)

### ExprOver

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `FrameClause`: [ExprFrameClause](#exprframeclause)?
  - `OrderBy`: [ExprOrderBy](#exprorderby)?
  - `Partitions`: IReadOnlyList<[ExprValue](#exprvalue)>?

### ExprParameter

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `ReplacedValue`: [ExprValue](#exprvalue)?
- Plain properties:
  - `TagName`: String?

### ExprPortableScalarFunction

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Arguments`: IReadOnlyList<[ExprValue](#exprvalue)>?
- Plain properties:
  - `PortableFunction`: PortableScalarFunction

### ExprPredicate

- Kind: abstract class
- Base: [ExprBoolean](#exprboolean)
- Direct descendants: [ExprExists](#exprexists), [ExprIn](#exprin), [ExprIsNull](#exprisnull), [ExprLike](#exprlike), [ExprPredicateLeftRight](#exprpredicateleftright)

### ExprPredicateLeftRight

- Kind: abstract class
- Base: [ExprPredicate](#exprpredicate)
- Direct descendants: [ExprBooleanEq](#exprbooleaneq), [ExprBooleanGt](#exprbooleangt), [ExprBooleanGtEq](#exprbooleangteq), [ExprBooleanLt](#exprbooleanlt), [ExprBooleanLtEq](#exprbooleanlteq), [ExprBooleanNotEq](#exprbooleannoteq)

### ExprQueryExpression

- Kind: class
- Base: [IExprQueryExpression](#iexprqueryexpression)
- Subnodes:
  - `Left`: [IExprSubQuery](#iexprsubquery)
  - `Right`: [IExprSubQuery](#iexprsubquery)
- Plain properties:
  - `QueryExpressionType`: ExprQueryExpressionType

### ExprQueryList

- Kind: class
- Base: [IExprQuery](#iexprquery)
- Subnodes:
  - `Expressions`: IReadOnlyList<[IExprComplete](#iexprcomplete)>

### ExprQuerySpecification

- Kind: class
- Base: [IExprQueryExpression](#iexprqueryexpression)
- Subnodes:
  - `From`: [IExprTableSource](#iexprtablesource)?
  - `GroupBy`: IReadOnlyList<[ExprValue](#exprvalue)>?
  - `SelectList`: IReadOnlyList<[IExprSelecting](#iexprselecting)>
  - `Top`: [ExprValue](#exprvalue)?
  - `Where`: [ExprBoolean](#exprboolean)?
- Plain properties:
  - `Distinct`: Boolean

### ExprScalarFunction

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Arguments`: IReadOnlyList<[ExprValue](#exprvalue)>?
  - `Name`: [ExprFunctionName](#exprfunctionname)
  - `Schema`: [ExprDbSchema](#exprdbschema)?

### ExprSchemaName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprSelect

- Kind: class
- Base: [IExprReadOnlyQuery](#iexprreadonlyquery)
- Subnodes:
  - `OrderBy`: [ExprOrderBy](#exprorderby)
  - `SelectQuery`: [IExprSubQuery](#iexprsubquery)

### ExprSelectOffsetFetch

- Kind: class
- Base: [IExprSubQuery](#iexprsubquery)
- Subnodes:
  - `OrderBy`: [ExprOrderByOffsetFetch](#exprorderbyoffsetfetch)
  - `SelectQuery`: [IExprSubQuery](#iexprsubquery)

### ExprSelecting

- Kind: abstract class
- Base: [IExprSelecting](#iexprselecting)
- Direct descendants: [ExprValue](#exprvalue)

### ExprSelectingValue

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Selecting`: [IExprSelecting](#iexprselecting)

### ExprStringConcat

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprStringLiteral

- Kind: class
- Base: [ExprLiteral](#exprliteral)
- Plain properties:
  - `Value`: String?

### ExprSub

- Kind: class
- Base: [ExprArithmetic](#exprarithmetic)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprSum

- Kind: class
- Base: [ExprArithmetic](#exprarithmetic)
- Subnodes:
  - `Left`: [ExprValue](#exprvalue)
  - `Right`: [ExprValue](#exprvalue)

### ExprTable

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Alias`: [ExprTableAlias](#exprtablealias)?
  - `FullName`: [IExprTableFullName](#iexprtablefullname)

### ExprTableAlias

- Kind: class
- Base: [IExprColumnSource](#iexprcolumnsource)
- Subnodes:
  - `Alias`: [IExprAlias](#iexpralias)

### ExprTableFullName

- Kind: class
- Base: [IExprTableFullName](#iexprtablefullname)
- Subnodes:
  - `DbSchema`: [ExprDbSchema](#exprdbschema)?
  - `TableName`: [ExprTableName](#exprtablename)

### ExprTableFunction

- Kind: class
- Base: [IExprTableSource](#iexprtablesource)
- Subnodes:
  - `Arguments`: IReadOnlyList<[ExprValue](#exprvalue)>?
  - `Name`: [ExprFunctionName](#exprfunctionname)
  - `Schema`: [ExprDbSchema](#exprdbschema)?

### ExprTableName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprTableValueConstructor

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Items`: IReadOnlyList<[ExprValueRow](#exprvaluerow)>

### ExprTempTableName

- Kind: class
- Base: [IExprName](#iexprname)
- Plain properties:
  - `Name`: String

### ExprType

- Kind: abstract class
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprTypeBoolean](#exprtypeboolean), [ExprTypeByte](#exprtypebyte), [ExprTypeByteArrayBase](#exprtypebytearraybase), [ExprTypeDateTime](#exprtypedatetime), [ExprTypeDateTimeOffset](#exprtypedatetimeoffset), [ExprTypeDecimal](#exprtypedecimal), [ExprTypeDouble](#exprtypedouble), [ExprTypeGuid](#exprtypeguid), [ExprTypeInt16](#exprtypeint16), [ExprTypeInt32](#exprtypeint32), [ExprTypeInt64](#exprtypeint64), [ExprTypeStringBase](#exprtypestringbase)

### ExprTypeBoolean

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeByte

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeByteArray

- Kind: class
- Base: [ExprTypeByteArrayBase](#exprtypebytearraybase)
- Plain properties:
  - `Size`: Int32?

### ExprTypeByteArrayBase

- Kind: abstract class
- Base: [ExprType](#exprtype)
- Direct descendants: [ExprTypeByteArray](#exprtypebytearray), [ExprTypeFixSizeByteArray](#exprtypefixsizebytearray)

### ExprTypeDateTime

- Kind: class
- Base: [ExprType](#exprtype)
- Plain properties:
  - `IsDate`: Boolean

### ExprTypeDateTimeOffset

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeDecimal

- Kind: class
- Base: [ExprType](#exprtype)
- Plain properties:
  - `PrecisionScale`: DecimalPrecisionScale?

### ExprTypeDouble

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeFixSizeByteArray

- Kind: class
- Base: [ExprTypeByteArrayBase](#exprtypebytearraybase)
- Plain properties:
  - `Size`: Int32

### ExprTypeFixSizeString

- Kind: class
- Base: [ExprTypeStringBase](#exprtypestringbase)
- Plain properties:
  - `IsUnicode`: Boolean
  - `Size`: Int32

### ExprTypeGuid

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeInt16

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeInt32

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeInt64

- Kind: class, singleton
- Base: [ExprType](#exprtype)

### ExprTypeString

- Kind: class
- Base: [ExprTypeStringBase](#exprtypestringbase)
- Plain properties:
  - `IsText`: Boolean
  - `IsUnicode`: Boolean
  - `Size`: Int32?

### ExprTypeStringBase

- Kind: abstract class
- Base: [ExprType](#exprtype)
- Direct descendants: [ExprTypeFixSizeString](#exprtypefixsizestring), [ExprTypeString](#exprtypestring), [ExprTypeXml](#exprtypexml)

### ExprTypeXml

- Kind: class, singleton
- Base: [ExprTypeStringBase](#exprtypestringbase)

### ExprUnboundedFrameBorder

- Kind: class
- Base: [ExprFrameBorder](#exprframeborder)
- Plain properties:
  - `FrameBorderDirection`: FrameBorderDirection

### ExprUnsafeValue

- Kind: class
- Base: [ExprValue](#exprvalue)
- Plain properties:
  - `UnsafeValue`: String

### ExprUpdate

- Kind: class
- Base: [IExprExec](#iexprexec)
- Subnodes:
  - `Filter`: [ExprBoolean](#exprboolean)?
  - `SetClause`: IReadOnlyList<[ExprColumnSetClause](#exprcolumnsetclause)>
  - `Source`: [IExprTableSource](#iexprtablesource)?
  - `Target`: [ExprTable](#exprtable)

### ExprValue

- Kind: abstract class
- Base: [ExprSelecting](#exprselecting)
- Direct descendants: [ExprArithmetic](#exprarithmetic), [ExprBitwise](#exprbitwise), [ExprCase](#exprcase), [ExprCaseWhenThen](#exprcasewhenthen), [ExprCast](#exprcast), [ExprColumn](#exprcolumn), [ExprDateAdd](#exprdateadd), [ExprDateDiff](#exprdatediff), [ExprFuncCoalesce](#exprfunccoalesce), [ExprFuncIsNull](#exprfuncisnull), [ExprGetDate](#exprgetdate), [ExprGetUtcDate](#exprgetutcdate), [ExprLiteral](#exprliteral), [ExprNull](#exprnull), [ExprParameter](#exprparameter), [ExprPortableScalarFunction](#exprportablescalarfunction), [ExprScalarFunction](#exprscalarfunction), [ExprSelectingValue](#exprselectingvalue), [ExprStringConcat](#exprstringconcat), [ExprUnsafeValue](#exprunsafevalue), [ExprValueQuery](#exprvaluequery)

### ExprValueFrameBorder

- Kind: class
- Base: [ExprFrameBorder](#exprframeborder)
- Subnodes:
  - `Value`: [ExprValue](#exprvalue)
- Plain properties:
  - `FrameBorderDirection`: FrameBorderDirection

### ExprValueQuery

- Kind: class
- Base: [ExprValue](#exprvalue)
- Subnodes:
  - `Query`: [IExprSubQuery](#iexprsubquery)

### ExprValueRow

- Kind: class
- Base: [IExpr](#iexpr)
- Subnodes:
  - `Items`: IReadOnlyList<[ExprValue](#exprvalue)>

### IExprAlias

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprAlias](#expralias), [ExprAliasGuid](#expraliasguid)

### IExprAssigning

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprDefault](#exprdefault)

### IExprColumnSource

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprTableAlias](#exprtablealias), [IExprTableFullName](#iexprtablefullname)

### IExprComplete

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [IExprExec](#iexprexec), [IExprQuery](#iexprquery)

### IExprExec

- Kind: interface
- Base: [IExprComplete](#iexprcomplete)
- Direct descendants: [ExprDelete](#exprdelete), [ExprIdentityInsert](#expridentityinsert), [ExprInsert](#exprinsert), [ExprList](#exprlist), [ExprMerge](#exprmerge), [ExprUpdate](#exprupdate)

### IExprInsertSource

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprInsertQuery](#exprinsertquery), [ExprInsertValues](#exprinsertvalues)

### IExprMergeMatched

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprMergeMatchedDelete](#exprmergematcheddelete), [ExprMergeMatchedUpdate](#exprmergematchedupdate)

### IExprMergeNotMatched

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprExprMergeNotMatchedInsert](#exprexprmergenotmatchedinsert), [ExprExprMergeNotMatchedInsertDefault](#exprexprmergenotmatchedinsertdefault)

### IExprName

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprColumnAlias](#exprcolumnalias), [ExprColumnName](#exprcolumnname), [ExprDatabaseName](#exprdatabasename), [ExprFunctionName](#exprfunctionname), [ExprSchemaName](#exprschemaname), [ExprTableName](#exprtablename), [ExprTempTableName](#exprtemptablename)

### IExprNamedSelecting

- Kind: interface
- Base: [IExprSelecting](#iexprselecting)
- Direct descendants: [ExprAliasedColumn](#expraliasedcolumn), [ExprAliasedColumnName](#expraliasedcolumnname), [ExprAliasedSelecting](#expraliasedselecting)

### IExprOutputColumn

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprOutputAction](#exproutputaction), [ExprOutputColumn](#exproutputcolumn), [ExprOutputColumnDeleted](#exproutputcolumndeleted), [ExprOutputColumnInserted](#exproutputcolumninserted)

### IExprQuery

- Kind: interface
- Base: [IExprComplete](#iexprcomplete)
- Direct descendants: [ExprDeleteOutput](#exprdeleteoutput), [ExprInsertOutput](#exprinsertoutput), [ExprQueryList](#exprquerylist), [IExprReadOnlyQuery](#iexprreadonlyquery)

### IExprQueryExpression

- Kind: interface
- Base: [IExprSubQuery](#iexprsubquery)
- Direct descendants: [ExprQueryExpression](#exprqueryexpression), [ExprQuerySpecification](#exprqueryspecification)

### IExprReadOnlyQuery

- Kind: interface
- Base: [IExprQuery](#iexprquery)
- Direct descendants: [ExprSelect](#exprselect), [IExprSubQuery](#iexprsubquery)

### IExprSelecting

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ExprAggregateFunction](#expraggregatefunction), [ExprAggregateOverFunction](#expraggregateoverfunction), [ExprAllColumns](#exprallcolumns), [ExprAnalyticFunction](#expranalyticfunction), [ExprSelecting](#exprselecting), [IExprNamedSelecting](#iexprnamedselecting)

### IExprSelectingSource

- Kind: interface
- Base: [IExpr](#iexpr)
- Direct descendants: [ISubQuerySource](#isubquerysource)

### IExprSubQuery

- Kind: interface
- Base: [IExprReadOnlyQuery](#iexprreadonlyquery)
- Direct descendants: [ExprSelectOffsetFetch](#exprselectoffsetfetch), [IExprQueryExpression](#iexprqueryexpression)

### IExprTableFullName

- Kind: interface
- Base: [IExprColumnSource](#iexprcolumnsource)
- Direct descendants: [ExprTableFullName](#exprtablefullname)

### IExprTableSource

- Kind: interface
- Base: [ISubQuerySource](#isubquerysource)
- Direct descendants: [ExprAliasedTableFunction](#expraliasedtablefunction), [ExprCrossedTable](#exprcrossedtable), [ExprCte](#exprcte), [ExprDerivedTable](#exprderivedtable), [ExprJoinedTable](#exprjoinedtable), [ExprLateralCrossedTable](#exprlateralcrossedtable), [ExprTable](#exprtable), [ExprTableFunction](#exprtablefunction)

### ISubQuerySource

- Kind: interface
- Base: [IExprSelectingSource](#iexprselectingsource)
- Direct descendants: [IExprTableSource](#iexprtablesource)

<!-- CodeGenEnd -->
