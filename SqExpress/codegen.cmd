@echo off

SET rootDir=%~dp0..\SqExpress.GenSyntaxTraversal

dotnet build "%rootDir%" -verbosity:quiet -noLogo -p:OutDir="%rootDir%\bin"

"%rootDir%\bin\SqExpress.GenSyntaxTraversal.exe" "%~dp0."