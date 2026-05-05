@echo off
echo ==========================================
echo Rayo - Hot Reload Development Mode
echo ==========================================
echo.
echo Este script inicia dos procesos:
echo 1. La aplicacion Rayo
echo 2. Watch mode para recompilar automaticamente
echo.
echo Presiona Ctrl+C para detener ambos
echo ==========================================
echo.

start "Rayo App" cmd /k "dotnet run"
timeout /t 3 /nobreak >nul
start "Rayo Watch" cmd /k "dotnet watch build --no-hot-reload"

echo.
echo Ambos procesos iniciados!
echo - Modifica Examples/MyUIBuilder.cs
echo - Guarda el archivo (Ctrl+S)
echo - Los cambios se veran automaticamente!
echo.
