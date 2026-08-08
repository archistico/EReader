@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
pushd "%ROOT%" >nul || exit /b 1

echo Pulizia cartelle bin e obj...
set /a REMOVED=0

for /d /r %%D in (bin) do (
  if exist "%%D" (
    echo   Rimuovo %%D
    rd /s /q "%%D"
    if exist "%%D" (
      echo ERROR: impossibile rimuovere %%D
      popd >nul
      exit /b 1
    )
    set /a REMOVED+=1 >nul
  )
)

for /d /r %%D in (obj) do (
  if exist "%%D" (
    echo   Rimuovo %%D
    rd /s /q "%%D"
    if exist "%%D" (
      echo ERROR: impossibile rimuovere %%D
      popd >nul
      exit /b 1
    )
    set /a REMOVED+=1 >nul
  )
)

echo Pulizia completata. Cartelle rimosse: %REMOVED%
popd >nul
exit /b 0
