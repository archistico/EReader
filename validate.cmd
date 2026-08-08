@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet non trovato nel PATH.
  exit /b 1
)

echo [1/12] Restore
dotnet restore EbookReader.sln
if errorlevel 1 exit /b 1

echo [2/12] Build Release
dotnet build EbookReader.sln -c Release --no-restore
if errorlevel 1 exit /b 1

echo [3/12] Test Release
dotnet test --solution EbookReader.sln -c Release --no-build
if errorlevel 1 exit /b 1

echo [4/12] CLI help smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --help >nul
if errorlevel 1 exit /b 1

echo [5/12] CLI version smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --version >nul
if errorlevel 1 exit /b 1

echo [6/12] CLI foundation-info smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --foundation-info
if errorlevel 1 exit /b 1

echo [7/12] First readable EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m1.0-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [8/12] M3.4 image EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.4-image-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [9/12] M3.5 hyperlink EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.5-link-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [10/12] M3.6 notes EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.6-notes-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [11/12] Library history smoke
set "EREADER_STATE_FILE=%TEMP%\ereader-m30-validation-%RANDOM%-%RANDOM%.json"
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --history >nul
set "_history_rc=%ERRORLEVEL%"
if exist "%EREADER_STATE_FILE%" del /q "%EREADER_STATE_FILE%" >nul 2>nul
set "EREADER_STATE_FILE="
if not "%_history_rc%"=="0" exit /b %_history_rc%

echo [12/12] Preferences config smoke
set "EREADER_CONFIG_FILE=%TEMP%\ereader-m33-config-%RANDOM%-%RANDOM%.json"
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --init-config >nul
if errorlevel 1 exit /b 1
if not exist "%EREADER_CONFIG_FILE%" (
  echo ERROR: --init-config non ha creato il file atteso.
  exit /b 1
)
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --config-path >nul
set "_config_rc=%ERRORLEVEL%"
if exist "%EREADER_CONFIG_FILE%" del /q "%EREADER_CONFIG_FILE%" >nul 2>nul
set "EREADER_CONFIG_FILE="
if not "%_config_rc%"=="0" exit /b %_config_rc%

echo.
echo M3.9 HOTFIX 1 VALIDATION PASSED
exit /b 0
