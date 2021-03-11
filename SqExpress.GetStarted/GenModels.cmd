@echo off
set root=%userprofile%\.nuget\packages\sqexpress

for /F "tokens=*" %%a in ('dir "%root%" /b /a:d /o:n') do set "lib=%root%\%%a"

set lib=%lib%\tools\codegen\SqExpress.CodeGenUtil.dll

dotnet "%lib%" genmodels -i "." -o ".\Models" -n "SqExpress.GetStarted.Models" --null-ref-types