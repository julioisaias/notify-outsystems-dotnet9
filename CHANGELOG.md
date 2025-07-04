# Changelog

Todos los cambios notables en este proyecto serán documentados en este archivo.

## [1.0.1] - 2024-01-03

### 🔧 Arreglos

- **Dependencias**: Removido paquete problemático `Microsoft.Toolkit.Win32.UI.Controls` que causaba errores de restauración NuGet
- **Notificaciones**: Simplificado el sistema de notificaciones para usar PowerShell en lugar de APIs directas de WinRT
- **Compatibilidad**: Mejorada la compatibilidad con diferentes versiones de Windows 10/11

### ✨ Mejoras

- **Notificaciones**: Implementado sistema de notificaciones toast nativo usando PowerShell
- **Fallback**: Mejorado el sistema de fallback para mostrar notificaciones en consola cuando no hay soporte
- **Configuración**: Actualizada configuración del proyecto para mejor soporte de WinRT
- **Logging**: Mejorados los mensajes de error y warnings para notificaciones

### 📖 Documentación

- **README**: Actualizada sección de solución de problemas con información sobre paquetes NuGet
- **README**: Clarificada información sobre el sistema de notificaciones

### ⚙️ Cambios Técnicos

- Removido `Microsoft.Toolkit.Win32.UI.Controls` v6.1.3 (no disponible)
- Actualizado `Microsoft.Windows.SDK.Contracts` a versión más estable
- Configurado `UseWinRT=true` en el proyecto
- Agregado `TargetPlatformIdentifier` y `TargetPlatformVersion` para Windows
- Implementado `SendWindowsNotificationAsync()` usando PowerShell
- Simplificado `IsNotificationSupported()` para verificar disponibilidad de PowerShell

## [1.0.0] - 2024-01-03

### 🎉 Release Inicial

- **Scraping**: Implementado scraping automático de OutSystems LifeTime usando Microsoft.Playwright
- **Autenticación**: Sistema de manejo de sesiones y cookies para evitar logins repetitivos
- **Base de Datos**: Persistencia en SQLite con Entity Framework Core
- **Monitoreo**: Detección de cambios cada 10 segundos (configurable)
- **Notificaciones**: Sistema de notificaciones nativas de Windows
- **Lógica de Negocio**: 
  - Procesamiento inteligente de detalles (SAM toma 2 palabras, otros 1 palabra)
  - Filtrado por ambientes Homologación y Producción
  - Cálculo de duración de deployments
- **Configuración**: URLs y credenciales configurables
- **Logging**: Sistema de logs con Serilog (consola y archivos)
- **Scripts**: Scripts de instalación para Windows (batch y PowerShell)

### 📁 Estructura Inicial

```
NotifyOutSystems/
├── Models/
│   ├── AppConfiguration.cs
│   └── DeploymentPlan.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Services/
│   ├── DatabaseService.cs
│   ├── ScrapingService.cs
│   ├── NotificationService.cs
│   └── MonitoringService.cs
├── Program.cs
├── appsettings.json
├── README.md
├── run.bat
└── run.ps1
```

### 🛠️ Tecnologías

- .NET 9.0
- Microsoft.Playwright para scraping
- Entity Framework Core con SQLite
- Serilog para logging
- Windows Toast Notifications
- Dependency Injection y Hosted Services 