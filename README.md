# OutSystems Monitor

Monitor de deployments para OutSystems LifeTime que detecta cambios de estado y envía notificaciones nativas de Windows.

## Características

- ✅ Scraping automático de la página de OutSystems LifeTime
- ✅ Autenticación con manejo de sesiones y cookies
- ✅ Persistencia en SQLite
- ✅ Notificaciones nativas de Windows 10/11 via PowerShell
- ✅ Monitoreo cada 10 segundos (configurable)
- ✅ Detección de cambios de estado
- ✅ Cálculo de duración de deployments
- ✅ Procesamiento inteligente de detalles
- ✅ Filtrado por ambientes (Homologación y Producción)

## Requisitos

- .NET 9.0 o superior
- Windows 10/11 (para notificaciones nativas)
- Acceso a OutSystems LifeTime

## Instalación

1. **Clonar o descargar el código**
   ```bash
   git clone <repository-url>
   cd NotifyOutSystems
   ```

2. **Restaurar dependencias**
   ```bash
   dotnet restore
   ```

3. **Instalar Playwright browsers**
   ```bash
   dotnet run --project . -- install chromium
   ```
   (Alternativamente, la aplicación instalará automáticamente los browsers en el primer inicio)

## Configuración

### 1. Configurar appsettings.json

Editar el archivo `appsettings.json` con tus credenciales:

```json
{
  "OutSystemsSettings": {
    "LoginUrl": "https://your-outsystems-url/lifetime",
    "StagingListUrl": "https://your-outsystems-url/lifetime/Stagings_List.aspx",
    "MonitoringIntervalSeconds": 10,
    "Username": "tu_usuario",
    "Password": "tu_contraseña",
    "EnableNotifications": true,
    "SessionTimeoutMinutes": 30
  }
}
```

### 2. Configurar URLs

Las URLs pueden ser configuradas para diferentes ambientes:

- **LoginUrl**: URL de login de OutSystems LifeTime
- **StagingListUrl**: URL de la lista de staging/deployment plans

### 3. Configurar Credenciales

⚠️ **Importante**: Por seguridad, considera usar variables de entorno para las credenciales:

```bash
# Windows
set OutSystemsSettings__Username=tu_usuario
set OutSystemsSettings__Password=tu_contraseña

# PowerShell
$env:OutSystemsSettings__Username = "tu_usuario"
$env:OutSystemsSettings__Password = "tu_contraseña"
```

## Uso

### Ejecutar la aplicación

```bash
dotnet run
```

### Ejecutar en modo release

```bash
dotnet run --configuration Release
```

### Ejecutar como servicio de Windows

```bash
# Compilar
dotnet publish -c Release -r win-x64 --self-contained

# Instalar como servicio (como administrador)
sc create "OutSystems Monitor" binpath="C:\ruta\a\NotifyOutSystems.exe"
sc start "OutSystems Monitor"
```

## Funcionamiento

### Lógica de Procesamiento

1. **Scraping**: Cada 10 segundos, la aplicación hace scraping de la página de staging
2. **Detección de cambios**: Compara el estado actual con el estado anterior almacenado en SQLite
3. **Procesamiento de detalles**:
   - Procesa los detalles del deployment para identificar el sistema o aplicación
4. **Filtrado de ambientes**: Solo notifica para "Homologation" y "Production"
5. **Notificaciones**: Envía notificaciones nativas de Windows cuando hay cambios

### Tipos de Notificaciones

- **Pase a Homologación**: Cuando hay cambios en deployments a "Homologation"
- **Pase a Producción**: Cuando hay cambios en deployments a "Production"
- **Estados soportados**: Running, Finished, etc.

### Mensajes de Notificación

- `"Sistema está haciendo un pase a Homologación"`
- `"Aplicación hizo un pase a Producción (Duración: 00:15:30)"`

## Base de Datos

La aplicación usa SQLite para almacenar:

- Planes de deployment
- Estados históricos
- Tiempos de inicio/fin
- Duraciones calculadas

La base de datos se crea automáticamente en `outsystems_monitoring.db`.

## Logs

Los logs se almacenan en:
- **Consola**: Información en tiempo real
- **Archivo**: `logs/outsystems-monitor-YYYY-MM-DD.log`

Niveles de log:
- **Information**: Operaciones normales
- **Warning**: Situaciones que requieren atención
- **Error**: Errores que no detienen la aplicación
- **Fatal**: Errores críticos que detienen la aplicación

## Solución de Problemas

### La aplicación no puede autenticarse

1. Verificar credenciales en `appsettings.json`
2. Verificar que las URLs sean correctas
3. Verificar conectividad de red
4. Revisar logs para errores específicos

### No se reciben notificaciones

1. Verificar que `EnableNotifications` esté en `true`
2. Verificar que estés en Windows 10/11
3. Verificar permisos de notificaciones del sistema
4. Revisar logs para errores de notificación

### Problemas con paquetes NuGet

Si encuentras errores con paquetes NuGet, prueba:

```bash
# Limpiar cache de NuGet
dotnet nuget locals all --clear

# Restaurar paquetes
dotnet restore --force
```

### Problemas con Playwright

```bash
# Reinstalar browsers
dotnet run --project . -- install chromium --force
```

### Base de datos corrupta

```bash
# Eliminar la base de datos (se recreará automáticamente)
del outsystems_monitoring.db
```

## Desarrollo

### Estructura del Proyecto

```
NotifyOutSystems/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── AppConfiguration.cs
│   └── DeploymentPlan.cs
├── Services/
│   ├── DatabaseService.cs
│   ├── ScrapingService.cs
│   ├── NotificationService.cs
│   └── MonitoringService.cs
├── Program.cs
├── appsettings.json
└── README.md
```

### Extender Funcionalidad

Para agregar nuevas características:

1. **Nuevos ambientes**: Modificar `DeploymentPlan.cs` y `MonitoringService.cs`
2. **Nuevos tipos de notificación**: Extender `NotificationService.cs`
3. **Nuevos campos de scraping**: Modificar `ScrapingService.cs`

## Licencia

Este proyecto está licenciado bajo [MIT License](LICENSE).
