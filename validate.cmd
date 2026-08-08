@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet non trovato nel PATH.
  exit /b 1
)

echo [1/16] Restore
dotnet restore EbookReader.sln
if errorlevel 1 exit /b 1

echo [2/16] Build Release
dotnet build EbookReader.sln -c Release --no-restore
if errorlevel 1 exit /b 1

echo [3/16] Test Release
dotnet test --solution EbookReader.sln -c Release --no-build
if errorlevel 1 exit /b 1

echo [4/16] CLI help smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --help >nul
if errorlevel 1 exit /b 1

echo [5/16] CLI version smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --version >nul
if errorlevel 1 exit /b 1

echo [6/16] CLI foundation-info smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --foundation-info
if errorlevel 1 exit /b 1

echo [7/16] First readable EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m1.0-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [8/16] M3.4 image EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.4-image-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [9/16] M3.5 hyperlink EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.5-link-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [10/16] M3.6 notes EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.6-notes-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [11/16] M3.10 degraded recovery smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.10-recovery-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [12/16] M3.11 link integrity smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m3.11-link-integrity-smoke.epub >nul
if errorlevel 1 exit /b 1


echo [13/16] M3.12 diagnostics modifier smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --diagnostics --plain test-books\m1.0-smoke.epub >nul
if errorlevel 1 exit /b 1

echo [14/16] M3.13 reliability corpus inventory
if not exist "test-books\reliability\README.md" (
  echo ERROR: README del corpus M3.13 mancante.
  exit /b 1
)
set "_corpus_count=0"
for %%F in (test-books\reliability\*.epub) do set /a _corpus_count+=1 >nul
if not "%_corpus_count%"=="20" (
  echo ERROR: corpus M3.13 atteso=20 EPUB, trovato=%_corpus_count%.
  exit /b 1
)

echo [15/16] Library history smoke
set "EREADER_STATE_FILE=%TEMP%\ereader-m30-validation-%RANDOM%-%RANDOM%.json"
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --history >nul
set "_history_rc=%ERRORLEVEL%"
if exist "%EREADER_STATE_FILE%" del /q "%EREADER_STATE_FILE%" >nul 2>nul
set "EREADER_STATE_FILE="
if not "%_history_rc%"=="0" exit /b %_history_rc%

echo [16/16] Preferences config smoke
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
echo M3.13 HOTFIX 1 VALIDATION PASSED
exit /b 0
