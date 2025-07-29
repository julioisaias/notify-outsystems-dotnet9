using Microsoft.Extensions.Logging;
using NotifyOutSystems.Models;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

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
    private readonly string _appId = "NotifyOutSystems";

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

    public Task SendNotificationAsync(string title, string message)
    {
        try
        {
            if (!_config.EnableNotifications)
            {
                _logger.LogInformation("Notificaciones deshabilitadas en configuración");
                return Task.CompletedTask;
            }

            if (!IsNotificationSupported())
            {
                _logger.LogWarning("Notificaciones no soportadas en este sistema");
                // Fallback: mostrar en consola
                Console.WriteLine($"[NOTIFICACIÓN] {title}: {message}");
                return Task.CompletedTask;
            }

            // Enviar notificación toast nativa de Windows
            SendToastNotification(title, message);

            _logger.LogInformation("Notificación toast enviada: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificación toast");
            
            // Fallback: mostrar en consola si las notificaciones fallan
            Console.WriteLine($"[NOTIFICACIÓN FALLBACK] {title}: {message}");
        }
        
        return Task.CompletedTask;
    }

    public bool IsNotificationSupported()
    {
        try
        {
            // Verificar si estamos en Windows 10/11 con soporte para notificaciones
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            if (Environment.OSVersion.Version.Major < 10)
            {
                return false;
            }

            // Verificar que las APIs de Windows Runtime estén disponibles
            var notificationManager = ToastNotificationManager.History;
            return notificationManager != null;
        }
        catch
        {
            return false;
        }
    }

    private void SendToastNotification(string title, string message)
    {
        try
        {
            // Crear el template XML para la notificación toast
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            
            // Obtener los elementos de texto
            var textElements = toastXml.GetElementsByTagName("text");
            
            // Configurar el título
            if (textElements.Count > 0)
            {
                textElements[0].AppendChild(toastXml.CreateTextNode(title));
            }
            
            // Configurar el mensaje
            if (textElements.Count > 1)
            {
                textElements[1].AppendChild(toastXml.CreateTextNode(message));
            }

            // Configurar audio
            var audioElement = toastXml.CreateElement("audio");
            audioElement.SetAttribute("src", "ms-winsoundevent:Notification.Default");
            audioElement.SetAttribute("loop", "false");
            
            var toastElement = toastXml.SelectSingleNode("/toast");
            if (toastElement != null)
            {
                toastElement.AppendChild(audioElement);
            }

            // Crear y configurar la notificación toast
            var toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(5);
            toast.Tag = "OutSystems";
            toast.Group = "Deployments";

            // Mostrar la notificación
            var notifier = ToastNotificationManager.CreateToastNotifier(_appId);
            notifier.Show(toast);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear notificación toast nativa");
            throw;
        }
    }
} 