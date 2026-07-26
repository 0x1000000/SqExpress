@echo off
setlocal

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "OUTDIRTOOLS=%ROOT%\tmp\tools\codegen"
set "OUTDIRPACK=%ROOT%\package"

if exist "%ROOT%\tmp" rmdir /s /q "%ROOT%\tmp"
if exist "%OUTDIRPACK%" rmdir /s /q "%OUTDIRPACK%"

dotnet build "%ROOT%\SqExpress.GenSyntaxTraversal\SqExpress.GenSyntaxTraversal.csproj" -c Release -verbosity:quiet -nologo -o "%ROOT%\SqExpress.GenSyntaxTraversal\bin\"
if errorlevel 1 exit /b %errorlevel%

dotnet "%ROOT%\SqExpress.GenSyntaxTraversal\bin\SqExpress.GenSyntaxTraversal.dll" "%ROOT%\SqExpress"
if errorlevel 1 exit /b %errorlevel%

dotnet test "%ROOT%\Test\SqExpress.Test\SqExpress.Test.csproj" -f net8.0
if errorlevel 1 exit /b %errorlevel%

dotnet build "%ROOT%\SqExpress.CodegenUtil\SqExpress.CodeGenUtil.csproj" -o "%OUTDIRTOOLS%" -c Release
if errorlevel 1 exit /b %errorlevel%

del /q "%OUTDIRTOOLS%\*.dev.json" 2>nul
del /q "%OUTDIRTOOLS%\*.pdb" 2>nul

dotnet pack "%ROOT%\SqExpress\SqExpress.csproj" -o "%OUTDIRPACK%" -c Release
if errorlevel 1 exit /b %errorlevel%

set "PACKAGE_ERROR=0"
if not exist "%OUTDIRPACK%\SqExpress.*.nupkg" (
    echo Expected NuGet package was not created in "%OUTDIRPACK%".
    set "PACKAGE_ERROR=1"
)
if not exist "%OUTDIRPACK%\SqExpress.*.snupkg" (
    echo Expected NuGet symbol package was not created in "%OUTDIRPACK%".
    set "PACKAGE_ERROR=1"
)

if exist "%ROOT%\tmp" rmdir /s /q "%ROOT%\tmp"

if "%PACKAGE_ERROR%"=="1" exit /b 1

endlocal
