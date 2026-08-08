@echo off
setlocal
title CassieWordCheck MSIX

set PROJ_DIR=%~dp0
set "WINUI_PROJ=%PROJ_DIR%CassieWordCheck.WinUI\CassieWordCheck.WinUI.csproj"

echo ========================================
echo   CassieWordCheck - WinUI 3 MSIX
echo   Platform: x64
echo   Version: 2.5.0
echo ========================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] dotnet not found.
    exit /b 1
)

pushd "%PROJ_DIR%"

echo [1/3] Restoring...
call dotnet restore "%WINUI_PROJ%" --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [2/3] Building WinUI...
call dotnet build "%WINUI_PROJ%" -c Release -p:Platform=x64 --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [3/3] Generating unsigned MSIX...
call dotnet build "%WINUI_PROJ%" -c Release -p:Platform=x64 -p:PublishProfile=Properties\PublishProfiles\win10-x64.pubxml -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false --verbosity quiet
if %errorlevel% neq 0 goto :err

echo.
echo MSIX output:
dir /s /b "%PROJ_DIR%CassieWordCheck.WinUI\bin\Release\AppPackages\*.msix"
popd
exit /b 0

:err
echo.
echo [FAILED]
popd
exit /b 1
