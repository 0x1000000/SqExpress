@echo off
setlocal

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "OUTDIR=%ROOT%\package"
set "STAGEDIR=%OUTDIR%\sqyra"
set "PACKAGE=%OUTDIR%\sqyra-0.0.1.tgz"

if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"
mkdir "%OUTDIR%"
if errorlevel 1 exit /b %errorlevel%

pushd "%ROOT%"
call npm run verify:package
if errorlevel 1 goto :error

node "%ROOT%\tools\prepare-package.mjs" "%STAGEDIR%"
if errorlevel 1 goto :error

call npm pack "%STAGEDIR%" --pack-destination "%OUTDIR%"
if errorlevel 1 goto :error

if not exist "%PACKAGE%" (
    echo Expected npm package was not created at "%PACKAGE%".
    goto :error
)

rmdir /s /q "%STAGEDIR%"
if exist "%STAGEDIR%" (
    echo Could not remove temporary package folder "%STAGEDIR%".
    goto :error
)

echo.
echo Package ready: "%PACKAGE%"
echo Publish with: npm publish "%PACKAGE%"
popd
endlocal
exit /b 0

:error
set "EXIT_CODE=%errorlevel%"
if "%EXIT_CODE%"=="0" set "EXIT_CODE=1"
if exist "%STAGEDIR%" rmdir /s /q "%STAGEDIR%"
popd
endlocal & exit /b %EXIT_CODE%
