@echo off
setlocal
cd /d "%~dp0"

if not exist "appsettings.Local.json" (
    echo Configurazione mancante.
    echo Copia appsettings.Local.example.json come appsettings.Local.json e imposta la password amministrativa.
    pause
    exit /b 1
)

findstr /c:"scegli-una-password-locale" "appsettings.Local.json" >nul
if not errorlevel 1 (
    echo Sostituisci la password di esempio in appsettings.Local.json prima di avviare ArciQuiz.
    pause
    exit /b 1
)

ArciQuiz.exe
set "exitCode=%ERRORLEVEL%"
echo.
echo ArciQuiz si e' arrestato con codice %exitCode%.
pause
exit /b %exitCode%
