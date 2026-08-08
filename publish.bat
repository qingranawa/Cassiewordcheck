@echo off
setlocal
title CassieWordCheck Velopack

pushd "%~dp0"

echo ========================================
echo   CassieWordCheck - Velopack
echo   Platform: x64
echo   Version: 2.5.1
echo ========================================
echo.

where pwsh >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] pwsh not found.
    set "exitCode=1"
    goto :finish
)

call pwsh -NoProfile -File "%~dp0scripts\pack-velopack.ps1" -Configuration Release -Version 2.5.1
set "exitCode=%errorlevel%"

:finish
popd
exit /b %exitCode%
