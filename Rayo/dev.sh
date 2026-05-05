#!/bin/bash

echo "=========================================="
echo "Rayo - Hot Reload Development Mode"
echo "=========================================="
echo ""
echo "Este script inicia dos procesos:"
echo "1. La aplicación Rayo"
echo "2. Watch mode para recompilar automáticamente"
echo ""
echo "Presiona Ctrl+C para detener ambos"
echo "=========================================="
echo ""

# Función para limpiar procesos al salir
cleanup() {
    echo ""
    echo "Deteniendo procesos..."
    kill $APP_PID $WATCH_PID 2>/dev/null
    exit
}

trap cleanup INT TERM

# Iniciar la aplicación en segundo plano
dotnet run &
APP_PID=$!

# Esperar un poco para que la app inicie
sleep 3

# Iniciar watch mode en segundo plano
dotnet watch build --no-hot-reload &
WATCH_PID=$!

echo ""
echo "Ambos procesos iniciados!"
echo "- Modifica Examples/MyUIBuilder.cs"
echo "- Guarda el archivo (Ctrl+S)"
echo "- Los cambios se verán automáticamente!"
echo ""
echo "Presiona Ctrl+C para detener"
echo ""

# Esperar a que terminen los procesos
wait
