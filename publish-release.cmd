@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "ROOT=%~dp0"
set "VERSION=%~1"

if "%VERSION%"=="" (
  echo Uso: publish-release.cmd VERSION
  echo Esempio: publish-release.cmd 1.0.0
  exit /b 2
)

if /i "%VERSION:~0,1%"=="v" set "VERSION=%VERSION:~1%"
if "%VERSION%"=="" (
  echo ERROR: versione non valida.
  exit /b 2
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: dotnet non trovato nel PATH.
  exit /b 1
)

where tar >nul 2>nul
if errorlevel 1 (
  echo ERROR: tar non trovato nel PATH. Windows 10/11 include normalmente tar.exe.
  exit /b 1
)

pushd "%ROOT%" >nul || exit /b 1

call clean.cmd
if errorlevel 1 goto :fail

call validate.cmd
if errorlevel 1 (
  echo.
  echo ERROR: gate di validazione fallito. Nessuna release viene prodotta.
  goto :fail
)

set "OUT=%ROOT%\artifacts\release\v%VERSION%"
if exist "%OUT%" rd /s /q "%OUT%"
mkdir "%OUT%" || goto :fail

call :publish win-x64
if errorlevel 1 goto :fail

call :publish linux-x64
if errorlevel 1 goto :fail

echo.
echo Release pronta in:
echo   %OUT%
echo.
echo Asset GitHub:
echo   EReader-%VERSION%-win-x64.zip
echo   EReader-%VERSION%-linux-x64.tar.gz
popd >nul
exit /b 0

:publish
set "RID=%~1"
set "STAGE=%OUT%\%RID%"

echo.
echo Publish %RID%...
dotnet publish src\EbookReader.Cli\EbookReader.Cli.csproj ^
  -c Release ^
  -r %RID% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugSymbols=false ^
  -p:DebugType=None ^
  -p:ContinuousIntegrationBuild=true ^
  -p:Version=%VERSION% ^
  -p:VersionSuffix= ^
  -o "%STAGE%"
if errorlevel 1 exit /b 1

copy /y README.md "%STAGE%\README.md" >nul || exit /b 1

if /i "%RID%"=="win-x64" (
  if exist "%STAGE%\ereader.exe" (
    "%STAGE%\ereader.exe" --version >nul
    if errorlevel 1 exit /b 1
  ) else (
    echo ERROR: ereader.exe non trovato nel publish win-x64.
    exit /b 1
  )

  pushd "%STAGE%" >nul || exit /b 1
  tar -a -c -f "%OUT%\EReader-%VERSION%-win-x64.zip" *
  set "ARCHIVE_RC=%ERRORLEVEL%"
  popd >nul
  if not "!ARCHIVE_RC!"=="0" exit /b !ARCHIVE_RC!
) else (
  pushd "%STAGE%" >nul || exit /b 1
  tar -czf "%OUT%\EReader-%VERSION%-%RID%.tar.gz" *
  set "ARCHIVE_RC=%ERRORLEVEL%"
  popd >nul
  if not "!ARCHIVE_RC!"=="0" exit /b !ARCHIVE_RC!
)

rd /s /q "%STAGE%"
exit /b 0

:fail
set "RC=%ERRORLEVEL%"
if "%RC%"=="0" set "RC=1"
popd >nul
exit /b %RC%
