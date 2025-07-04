using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotifyOutSystems.Models;

namespace NotifyOutSystems.Services;

public interface IMonitoringService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<bool> CheckDeploymentChangesAsync();
}

public class MonitoringService : BackgroundService, IMonitoringService
{
    private readonly IScrapingService _scrapingService;
    private readonly IDatabaseService _databaseService;
    private readonly INotificationService _notificationService;
    private readonly AppConfiguration _config;
    private readonly ILogger<MonitoringService> _logger;
    private readonly Timer _timer;
    private bool _isRunning = false;

    public MonitoringService(
        IScrapingService scrapingService,
        IDatabaseService databaseService,
        INotificationService notificationService,
        AppConfiguration config,
        ILogger<MonitoringService> logger)
    {
        _scrapingService = scrapingService;
        _databaseService = databaseService;
        _notificationService = notificationService;
        _config = config;
        _logger = logger;
        
        // Configurar timer para chequeos periódicos
        _timer = new Timer(async _ => await CheckDeploymentChangesAsync(), 
            null, Timeout.Infinite, Timeout.Infinite);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando servicio de monitoreo OutSystems");
        
        try
        {
            // Inicializar base de datos
            await _databaseService.InitializeDatabaseAsync();
            
            // Verificar soporte de notificaciones
            if (_notificationService.IsNotificationSupported())
            {
                _logger.LogInformation("Notificaciones de Windows soportadas");
                await _notificationService.SendNotificationAsync("OutSystems Monitor", 
                    "Servicio de monitoreo iniciado correctamente");
            }
            else
            {
                _logger.LogWarning("Notificaciones de Windows no soportadas. Solo se mostrarán logs.");
            }

            // Iniciar chequeo periódico
            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_config.MonitoringIntervalSeconds));
            
            _logger.LogInformation("Servicio de monitoreo iniciado. Intervalo: {Seconds} segundos", 
                _config.MonitoringIntervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar el servicio de monitoreo");
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deteniendo servicio de monitoreo OutSystems");
        
        // Detener el timer
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // Limpiar recursos
        _scrapingService?.Dispose();
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("Servicio de monitoreo detenido");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.MonitoringIntervalSeconds), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Esperado cuando se cancela el token
                break;
            }
        }
    }

    public async Task<bool> CheckDeploymentChangesAsync()
    {
        // Prevenir ejecuciones concurrentes
        if (_isRunning)
        {
            _logger.LogDebug("Chequeo ya en progreso, saltando...");
            return false;
        }

        _isRunning = true;
        
        try
        {
            _logger.LogDebug("Iniciando chequeo de cambios en deployments");
            
            // Realizar scraping
            var currentDeployments = await _scrapingService.ScrapeDeploymentPlansAsync();
            
            if (currentDeployments.Count == 0)
            {
                _logger.LogWarning("No se encontraron deployments en el scraping");
                return false;
            }

            _logger.LogDebug("Scraping completado. Encontrados {Count} deployments", currentDeployments.Count);

            // Procesar cada deployment
            var changesDetected = false;
            
            foreach (var deployment in currentDeployments)
            {
                try
                {
                    var savedDeployment = await _databaseService.SaveDeploymentPlanAsync(deployment);
                    
                    if (savedDeployment.HasStatusChanged)
                    {
                        changesDetected = true;
                        _logger.LogInformation("Cambio detectado en {PlanName} -> {DeployedTo}: {PreviousStatus} -> {CurrentStatus}",
                            savedDeployment.PlanName, savedDeployment.DeployedTo, 
                            savedDeployment.PreviousStatus, savedDeployment.Status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar deployment {PlanName}", deployment.PlanName);
                }
            }

            // Procesar notificaciones pendientes
            await ProcessPendingNotificationsAsync();

            if (changesDetected)
            {
                _logger.LogInformation("Chequeo completado. Se detectaron cambios en los deployments.");
            }
            else
            {
                _logger.LogDebug("Chequeo completado. No se detectaron cambios.");
            }

            return changesDetected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el chequeo de cambios");
            return false;
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task ProcessPendingNotificationsAsync()
    {
        try
        {
            var changedDeployments = await _databaseService.GetChangedDeploymentPlansAsync();
            
            if (changedDeployments.Count == 0)
            {
                return;
            }

            _logger.LogDebug("Procesando {Count} notificaciones pendientes", changedDeployments.Count);

            foreach (var deployment in changedDeployments)
            {
                try
                {
                    // Filtrar solo los deployments relevantes (Homologation y Production)
                    if (deployment.IsHomologation || deployment.IsProduction)
                    {
                        await _notificationService.SendNotificationAsync(deployment);
                        await _databaseService.MarkNotificationAsSentAsync(deployment.Id);
                        
                        _logger.LogInformation("Notificación enviada para {ProcessedDetails} -> {DeployedTo}: {Status}",
                            deployment.ProcessedDetails, deployment.DeployedTo, deployment.Status);
                    }
                    else
                    {
                        // Para otros ambientes, solo marcar como procesado sin enviar notificación
                        await _databaseService.MarkNotificationAsSentAsync(deployment.Id);
                        
                        _logger.LogDebug("Cambio registrado (sin notificación) para {ProcessedDetails} -> {DeployedTo}: {Status}",
                            deployment.ProcessedDetails, deployment.DeployedTo, deployment.Status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar notificación para {PlanName}", deployment.PlanName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar notificaciones pendientes");
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        _scrapingService?.Dispose();
        base.Dispose();
    }
} 