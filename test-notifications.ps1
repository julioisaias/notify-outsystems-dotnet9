#!/usr/bin/env pwsh

Write-Host "=== Test de Notificaciones OutSystems ===" -ForegroundColor Green
Write-Host "Probando diferentes escenarios de notificación..." -ForegroundColor Yellow

# Ejecutar pruebas de notificación específicas
Write-Host "`n1. Probando notificación simple (SAM aplicación)..." -ForegroundColor Cyan
dotnet run --project NotifyOutSystems.csproj TestNotifications single

Write-Host "`n2. Probando notificación múltiples aplicaciones..." -ForegroundColor Cyan  
dotnet run --project NotifyOutSystems.csproj TestNotifications multiple

Write-Host "`n3. Probando notificación finalizado exitosamente..." -ForegroundColor Cyan
dotnet run --project NotifyOutSystems.csproj TestNotifications finished

Write-Host "`n4. Probando notificación con duración..." -ForegroundColor Cyan
dotnet run --project NotifyOutSystems.csproj TestNotifications duration

Write-Host "`n5. Probando notificación aplicación estándar..." -ForegroundColor Cyan
dotnet run --project NotifyOutSystems.csproj TestNotifications standard

Write-Host "`n=== Pruebas completadas ===" -ForegroundColor Green

Write-Host ""
Write-Host "📝 Información adicional:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Para mejorar las notificaciones toast, puedes instalar BurntToast:" -ForegroundColor Yellow
Write-Host "  Install-Module -Name BurntToast -Force" -ForegroundColor Gray
Write-Host ""
Write-Host "Si las notificaciones toast no funcionan, se usará msg.exe como fallback." -ForegroundColor Yellow
Write-Host ""

Read-Host "Presiona Enter para salir" 