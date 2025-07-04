#!/usr/bin/env pwsh

Write-Host "===================================" -ForegroundColor Green
Write-Host "    OutSystems Monitor - Setup" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""

# Verificar si .NET está instalado
try {
    $dotnetVersion = dotnet --version
    Write-Host "[1/4] Verificando .NET instalado..." -ForegroundColor Yellow
    Write-Host "✓ .NET $dotnetVersion detectado" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: .NET no está instalado o no está en el PATH" -ForegroundColor Red
    Write-Host "Por favor, instala .NET 9.0 o superior desde https://dotnet.microsoft.com/download" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host ""

# Restaurar paquetes
Write-Host "[2/4] Restaurando paquetes NuGet..." -ForegroundColor Yellow
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Falló la restauración de paquetes"
    }
    Write-Host "✓ Paquetes restaurados correctamente" -ForegroundColor Green
} catch {
    Write-Host "✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host ""

# Instalar Playwright browsers
Write-Host "[3/4] Instalando Playwright browsers..." -ForegroundColor Yellow
try {
    dotnet run --project . -- install chromium
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠ ADVERTENCIA: Falló la instalación de Playwright browsers" -ForegroundColor Yellow
        Write-Host "La aplicación intentará instalarlos automáticamente" -ForegroundColor Yellow
    } else {
        Write-Host "✓ Playwright browsers instalados correctamente" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ ADVERTENCIA: Error al instalar Playwright browsers" -ForegroundColor Yellow
    Write-Host "La aplicación intentará instalarlos automáticamente" -ForegroundColor Yellow
}

Write-Host ""

# Verificar configuración
Write-Host "[4/4] Verificando configuración..." -ForegroundColor Yellow
if (-not (Test-Path "appsettings.json")) {
    Write-Host "✗ No se encontró appsettings.json" -ForegroundColor Red
    if (Test-Path "appsettings.Example.json") {
        Write-Host "Copiando archivo de ejemplo..." -ForegroundColor Yellow
        Copy-Item "appsettings.Example.json" "appsettings.json"
        Write-Host ""
        Write-Host "IMPORTANTE: Edita appsettings.json y configura tu usuario y contraseña" -ForegroundColor Red
        Write-Host "Archivo copiado: appsettings.json" -ForegroundColor Yellow
        Write-Host ""
        Read-Host "Presiona Enter después de configurar las credenciales"
    } else {
        Write-Host "✗ ERROR: Tampoco se encontró appsettings.Example.json" -ForegroundColor Red
        Read-Host "Presiona Enter para salir"
        exit 1
    }
} else {
    Write-Host "✓ Configuración encontrada" -ForegroundColor Green
}

Write-Host ""

# Iniciar aplicación
Write-Host "===================================" -ForegroundColor Green
Write-Host "    Iniciando OutSystems Monitor" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green
Write-Host ""
Write-Host "Presiona Ctrl+C para detener la aplicación" -ForegroundColor Yellow
Write-Host ""

try {
    dotnet run --configuration Release
} catch {
    Write-Host ""
    Write-Host "✗ ERROR: La aplicación finalizó con errores" -ForegroundColor Red
    Write-Host "Revisa los logs para más detalles" -ForegroundColor Red
    Read-Host "Presiona Enter para salir"
    exit 1
}

Write-Host ""
Write-Host "Aplicación finalizada." -ForegroundColor Green
Read-Host "Presiona Enter para salir" 