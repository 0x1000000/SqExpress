#!/bin/bash

set -e

ROOT=`dirname $0`
OUTDIRTOOLS="$ROOT/tmp/tools/codegen"
OUTDIRPACK="$ROOT/package"

rm -rf "$ROOT/tmp"
rm -rf "$OUTDIRPACK"

dotnet build "$ROOT/SqExpress.GenSyntaxTraversal/SqExpress.GenSyntaxTraversal.csproj" -c Release -verbosity:quiet -noLogo -o "$ROOT/SqExpress.GenSyntaxTraversal/bin/"

dotnet "$ROOT/SqExpress.GenSyntaxTraversal/bin/SqExpress.GenSyntaxTraversal.dll" "$ROOT/SqExpress"

dotnet test "$ROOT/Test/SqExpress.Test/SqExpress.Test.csproj" -f "net8.0"

dotnet build "$ROOT/SqExpress.CodegenUtil/SqExpress.CodeGenUtil.csproj" -o "$OUTDIRTOOLS" -c Release

rm -rf "$OUTDIRTOOLS"/*.dev.json

dotnet pack "$ROOT/SqExpress/SqExpress.csproj" -o "$OUTDIRPACK" -c Release

rm -rf "$ROOT/tmp"
