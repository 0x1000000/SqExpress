#!/bin/bash

set -e

ROOT=`dirname $0`
OUTDIRTOOLS="$ROOT/tmp/tools/codegen"
OUTDIRPACK="$ROOT/package"

rm -rf "$ROOT/tmp"
rm -rf "$OUTDIRPACK"

dotnet test "$ROOT/Test/SqExpress.Test/SqExpress.Test.csproj"

dotnet build "$ROOT/SqExpress.CodegenUtil/SqExpress.CodegenUtil.csproj" -o "$OUTDIRTOOLS" -c Release

rm -rf "$OUTDIRTOOLS"/*.dev.json

dotnet pack "$ROOT/SqExpress/SqExpress.csproj" -o "$OUTDIRPACK" -c Release

rm -rf "$ROOT/tmp"
