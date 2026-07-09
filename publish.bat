@echo off
setlocal enabledelayedexpansion
title CassieWordCheck Publish

set PROJ_DIR=%~dp0
set PUBLISH_DIR=%PROJ_DIR%dist
set APP_NAME=CassieWordCheck
set RID=win-x64
:: 保存脚本路径（call :label 后会丢失 %~dp0）
set SCRIPT_DIR=%~dp0

echo ========================================
echo   CassieWordCheck - Publish Package
echo   Runtime: %RID% (Self-contained)
echo   Version: 2.4.1
echo ========================================
echo.

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [ERROR] dotnet not found.
    pause
    exit /b 1
)

pushd "%PROJ_DIR%"

if exist "%PUBLISH_DIR%" (
    echo [1/5] Cleaning...
    rmdir /s /q "%PUBLISH_DIR%" 2>nul
)

echo [2/5] Restoring...
call dotnet restore "%APP_NAME%.csproj" --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [3/5] Building...
call dotnet build "%APP_NAME%.csproj" -c Release --verbosity quiet
if %errorlevel% neq 0 goto :err

echo [4/5] Publishing...
call dotnet publish "%APP_NAME%.csproj" -c Release -r %RID% -o "%PUBLISH_DIR%" --verbosity normal
if %errorlevel% neq 0 goto :err

if exist "%PUBLISH_DIR%\*.pdb" del /q "%PUBLISH_DIR%\*.pdb" 2>nul

echo [5/5] Organizing binaries...
call :organize_binaries "%PUBLISH_DIR%"

echo.
echo ====== Output ======
dir /b "%PUBLISH_DIR%data\" 2>nul
if exist "%PUBLISH_DIR%Resources\" (
    echo   [res] Resources\Locales\
)
echo ====================
echo.
echo Done!
echo.
start "" "%PUBLISH_DIR%"
goto :end

:err
echo.
echo [FAILED]
pause
exit /b 1

:end
endlocal
exit /b 0

:: ── 发布目录整理：把 DLL 分类到 natives/ 和 runtimes/ ─────────
:organize_binaries
powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%scripts\pack-dist.ps1" -TargetDir "%~1"
if %errorlevel% neq 0 (
    echo   [WARN] 整理过程遇到错误，但发布已完成。
)
exit /b 0