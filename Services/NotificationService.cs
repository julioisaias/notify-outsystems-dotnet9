using Microsoft.Extensions.Logging;
using NotifyOutSystems.Models;
using System.Diagnostics;

namespace NotifyOutSystems.Services;

public interface INotificationService
{
    Task SendNotificationAsync(DeploymentPlan deploymentPlan);
    Task SendNotificationAsync(string title, string message);
    bool IsNotificationSupported();
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    private readonly AppConfiguration _config;

    public NotificationService(ILogger<NotificationService> logger, AppConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task SendNotificationAsync(DeploymentPlan deploymentPlan)
    {
        try
        {
            if (!_config.EnableNotifications)
            {
                _logger.LogInformation("Notificaciones deshabilitadas en configuración");
                return;
            }

            if (!IsNotificationSupported())
            {
                _logger.LogWarning("Notificaciones no soportadas en este sistema");
                return;
            }

            var title = "OutSystems Deployment";
            var message = deploymentPlan.GetNotificationMessage();

            await SendNotificationAsync(title, message);
            
            _logger.LogInformation("Notificación enviada para: {PlanName} -> {DeployedTo}", 
                deploymentPlan.PlanName, deploymentPlan.DeployedTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación para {PlanName}", deploymentPlan.PlanName);
        }
    }

    public async Task SendNotificationAsync(string title, string message)
    {
        try
        {
            if (!_config.EnableNotifications)
            {
                _logger.LogInformation("Notificaciones deshabilitadas en configuración");
                return;
            }

            if (!IsNotificationSupported())
            {
                _logger.LogWarning("Notificaciones no soportadas en este sistema");
                // Fallback: mostrar en consola
                Console.WriteLine($"[NOTIFICACIÓN] {title}: {message}");
                return;
            }

            // Usar PowerShell para enviar notificación de Windows 10/11
            await SendWindowsNotificationAsync(title, message);

            _logger.LogInformation("Notificación enviada: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación");
            
            // Fallback: mostrar en consola si las notificaciones fallan
            Console.WriteLine($"[NOTIFICACIÓN] {title}: {message}");
        }
    }

    public bool IsNotificationSupported()
    {
        try
        {
            // Verificar si estamos en Windows 10/11
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            if (Environment.OSVersion.Version.Major < 10)
            {
                return false;
            }

            // Verificar si PowerShell está disponible
            return File.Exists(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe");
        }
        catch
        {
            return false;
        }
    }

    private async Task SendWindowsNotificationAsync(string title, string message)
    {
        try
        {
            // Escapar caracteres especiales para PowerShell
            var escapedTitle = title.Replace("'", "''").Replace("`", "``");
            var escapedMessage = message.Replace("'", "''").Replace("`", "``");
            
            // Script de PowerShell para mostrar notificación usando BurntToast
            var script = $@"
                # Intentar usar BurntToast si está disponible, sino usar método simple
                try {{
                    Import-Module BurntToast -ErrorAction Stop
                    New-BurntToastNotification -Text '{escapedTitle}', '{escapedMessage}' -AppLogo 'https://via.placeholder.com/48x48.png?text=OS'
                }} catch {{
                    # Fallback: usar msg.exe para mostrar un mensaje simple
                    msg.exe * /time:10 '{escapedTitle}: {escapedMessage}'
                }}
            ";

            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("PowerShell notification failed: {Error}", error);
                    throw new Exception($"PowerShell notification failed: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Windows notification via PowerShell");
            throw;
        }
    }
} 