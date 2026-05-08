@echo off
set MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
if exist %MSBUILD% (
    echo Building the project...
    %MSBUILD% autorun.csproj /p:Configuration=Release /v:minimal
    if %errorlevel% neq 0 (
        echo.
        echo BUILD FAILED!
        pause
        exit /b %errorlevel%
    )
    echo.
    echo Build successful! Starting the app...
    start "" "bin\Release\autorun.exe"
) else (
    echo MSBuild not found at %MSBUILD%
    pause
)
