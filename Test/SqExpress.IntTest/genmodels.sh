#!/bin/bash
lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll
dotnet $lib genmodels -i "./Tables" -o "./Tables/Models" -n "SqExpress.IntTest.Tables.Models" -v detailed --null-ref-types --rw-classes