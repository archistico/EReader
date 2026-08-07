@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet non trovato nel PATH.
  exit /b 1
)

echo [1/8] Restore
dotnet restore EbookReader.sln
if errorlevel 1 exit /b 1

echo [2/8] Build Release
dotnet build EbookReader.sln -c Release --no-restore
if errorlevel 1 exit /b 1

echo [3/8] Test Release
dotnet test --solution EbookReader.sln -c Release --no-build
if errorlevel 1 exit /b 1

echo [4/8] CLI help smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --help >nul
if errorlevel 1 exit /b 1

echo [5/8] CLI version smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --version >nul
if errorlevel 1 exit /b 1

echo [6/8] CLI foundation-info smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --foundation-info
if errorlevel 1 exit /b 1

echo [7/8] First readable EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m1.0-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [8/8] Library history smoke
set "EREADER_STATE_FILE=%TEMP%\ereader-m30-validation-%RANDOM%-%RANDOM%.json"
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --history >nul
set "_history_rc=%ERRORLEVEL%"
if exist "%EREADER_STATE_FILE%" del /q "%EREADER_STATE_FILE%" >nul 2>nul
set "EREADER_STATE_FILE="
if not "%_history_rc%"=="0" exit /b %_history_rc%

echo.
echo M3.1 HOTFIX 1 VALIDATION PASSED
exit /b 0
