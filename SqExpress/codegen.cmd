@echo off

SET rootDir=%~dp0..\SqExpress.GenSyntaxTraversal

dotnet build "%rootDir%" -verbosity:quiet -noLogo -p:OutDir="%rootDir%\bin"

dotnet "%rootDir%\bin\SqExpress.GenSyntaxTraversal.dll" "%~dp0."