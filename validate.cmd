@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet non trovato nel PATH.
  exit /b 1
)

echo [1/7] Restore
dotnet restore EbookReader.sln
if errorlevel 1 exit /b 1

echo [2/7] Build Release
dotnet build EbookReader.sln -c Release --no-restore
if errorlevel 1 exit /b 1

echo [3/7] Test Release
dotnet test --solution EbookReader.sln -c Release --no-build
if errorlevel 1 exit /b 1

echo [4/7] CLI help smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --help >nul
if errorlevel 1 exit /b 1

echo [5/7] CLI version smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --version >nul
if errorlevel 1 exit /b 1

echo [6/7] CLI foundation-info smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --foundation-info
if errorlevel 1 exit /b 1

echo [7/7] First readable EPUB plain smoke
dotnet run --project src\EbookReader.Cli\EbookReader.Cli.csproj -c Release --no-build -- --plain test-books\m1.0-smoke.epub >nul
if errorlevel 1 exit /b 1

echo.
echo M2.3 HOTFIX 1 VALIDATION PASSED
exit /b 0
