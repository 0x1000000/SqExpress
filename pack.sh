#!/bin/bash

set -e

ROOT=`dirname $0`
OUTDIRTOOLS="$ROOT/tmp/tools/codegen"
OUTDIRPACK="$ROOT/package"

rm -rf "$ROOT/tmp"
rm -rf "$OUTDIRPACK"

dotnet test "$ROOT/Test/SqExpress.Test/SqExpress.Test.csproj" -f "netcoreapp3.1"

dotnet build "$ROOT/SqExpress.CodegenUtil/SqExpress.CodeGenUtil.csproj" -o "$OUTDIRTOOLS" -c Release

rm -rf "$OUTDIRTOOLS"/*.dev.json

dotnet pack "$ROOT/SqExpress/SqExpress.csproj" -o "$OUTDIRPACK" -c Release

rm -rf "$ROOT/tmp"
