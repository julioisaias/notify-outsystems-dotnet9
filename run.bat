@echo off
echo ===================================
echo    OutSystems Monitor - Setup
echo ===================================
echo.

:: Verificar si .NET 9.0 está instalado
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET no está instalado o no está en el PATH
    echo Por favor, instala .NET 9.0 o superior desde https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/4] Verificando .NET instalado...
dotnet --version
echo.

echo [2/4] Restaurando paquetes NuGet...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Falló la restauración de paquetes
    pause
    exit /b 1
)
echo.

echo [3/4] Instalando Playwright browsers...
dotnet run --project . -- install chromium
if %errorlevel% neq 0 (
    echo ADVERTENCIA: Falló la instalación de Playwright browsers
    echo La aplicación intentará instalarlos automáticamente
)
echo.

echo [4/4] Verificando configuración...
if not exist "appsettings.json" (
    echo ERROR: No se encontró appsettings.json
    if exist "appsettings.Example.json" (
        echo Copiando archivo de ejemplo...
        copy "appsettings.Example.json" "appsettings.json"
        echo.
        echo IMPORTANTE: Edita appsettings.json y configura tu usuario y contraseña
        echo Presiona cualquier tecla para continuar después de configurar las credenciales...
        pause
    ) else (
        echo ERROR: Tampoco se encontró appsettings.Example.json
        pause
        exit /b 1
    )
)
echo.

echo ===================================
echo    Iniciando OutSystems Monitor
echo ===================================
echo.
echo Presiona Ctrl+C para detener la aplicación
echo.

dotnet run --configuration Release

if %errorlevel% neq 0 (
    echo.
    echo ERROR: La aplicación finalizó con errores
    echo Revisa los logs para más detalles
    pause
)

echo.
echo Aplicación finalizada.
pause 