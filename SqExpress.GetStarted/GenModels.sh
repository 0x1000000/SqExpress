#!/bin/bash
lib=~/.nuget/packages/sqexpress/$(ls ~/.nuget/packages/sqexpress -r|head -n 1)/tools/codegen/SqExpress.CodeGenUtil.dll
dotnet $lib genmodels -i "." -o "./Models" -n "SqExpress.GetStarted.Models" --null-ref-types