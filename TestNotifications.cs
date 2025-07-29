using Microsoft.Extensions.Logging;
using NotifyOutSystems.Models;
using NotifyOutSystems.Services;

namespace NotifyOutSystems;

public static class TestNotifications
{
    public static async Task RunTests(string[] args)
    {
        Console.WriteLine("=== Test de Notificaciones OutSystems ===");
        Console.WriteLine();

        // Configurar logging simple para pruebas
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<NotificationService>();
        
        // Configuración básica para pruebas
        var config = new AppConfiguration
        {
            EnableNotifications = true
        };

        var notificationService = new NotificationService(logger, config);

        // Verificar soporte de notificaciones
        Console.WriteLine($"🔔 Soporte de notificaciones: {(notificationService.IsNotificationSupported() ? "✅ Sí" : "❌ No")}");
        Console.WriteLine();

        // Ejecutar prueba específica si se proporciona
        if (args.Length > 1)
        {
            await RunSpecificTest(notificationService, args[1]);
        }
        else
        {
            await RunAllTests(notificationService);
        }

        Console.WriteLine();
        Console.WriteLine("=== Pruebas completadas ===");
    }

    private static async Task RunSpecificTest(NotificationService notificationService, string testType)
    {
        switch (testType.ToLower())
        {
            case "single":
                await TestSingleApplication(notificationService);
                break;
            case "multiple":
                await TestMultipleApplications(notificationService);
                break;
            case "finished":
                await TestFinishedSuccessfully(notificationService);
                break;
            case "duration":
                await TestWithDuration(notificationService);
                break;
            case "standard":
                await TestStandardApplication(notificationService);
                break;
            default:
                Console.WriteLine($"❌ Tipo de prueba desconocido: {testType}");
                break;
        }
    }

    private static async Task RunAllTests(NotificationService notificationService)
    {
        await TestSingleApplication(notificationService);
        await TestMultipleApplications(notificationService);
        await TestFinishedSuccessfully(notificationService);
        await TestWithDuration(notificationService);
        await TestStandardApplication(notificationService);
    }

    private static async Task TestSingleApplication(NotificationService notificationService)
    {
        Console.WriteLine("🧪 Test 1: Aplicación simple iniciando...");
        
        var deployment = new DeploymentPlan
        {
            PlanName = "Test Plan 1",
            DeployedTo = "Homologation",
            Status = "Running",
            Details = "MiApp",
            ProcessedDetails = "MiApp",
            LastUpdated = DateTime.Now
        };

        await notificationService.SendNotificationAsync(deployment);
        Console.WriteLine("✅ Notificación enviada");
        await Task.Delay(2000);
    }

    private static async Task TestMultipleApplications(NotificationService notificationService)
    {
        Console.WriteLine("🧪 Test 2: Múltiples aplicaciones iniciando...");
        
        var deployment = new DeploymentPlan
        {
            PlanName = "Test Plan 2",
            DeployedTo = "Production",
            Status = "Running",
            Details = "App1, App2, App3, App4",
            ProcessedDetails = "Varias aplicaciones",
            LastUpdated = DateTime.Now
        };

        await notificationService.SendNotificationAsync(deployment);
        Console.WriteLine("✅ Notificación enviada");
        await Task.Delay(2000);
    }

    private static async Task TestFinishedSuccessfully(NotificationService notificationService)
    {
        Console.WriteLine("🧪 Test 3: Aplicación finalizada exitosamente...");
        
        var deployment = new DeploymentPlan
        {
            PlanName = "Test Plan 3",
            DeployedTo = "Homologation",
            Status = "Finished Successfully",
            Details = "MiApp",
            ProcessedDetails = "MiApp",
            LastUpdated = DateTime.Now
        };

        await notificationService.SendNotificationAsync(deployment);
        Console.WriteLine("✅ Notificación enviada");
        await Task.Delay(2000);
    }

    private static async Task TestWithDuration(NotificationService notificationService)
    {
        Console.WriteLine("🧪 Test 4: Múltiples aplicaciones finalizadas con duración...");
        
        var deployment = new DeploymentPlan
        {
            PlanName = "Test Plan 4",
            DeployedTo = "Production",
            Status = "Finished Successfully",
            Details = "App1, App2, App3",
            ProcessedDetails = "Varias aplicaciones",
            Duration = TimeSpan.FromMinutes(12).Add(TimeSpan.FromSeconds(35)),
            LastUpdated = DateTime.Now
        };

        await notificationService.SendNotificationAsync(deployment);
        Console.WriteLine("✅ Notificación enviada");
        await Task.Delay(2000);
    }

    private static async Task TestStandardApplication(NotificationService notificationService)
    {
        Console.WriteLine("🧪 Test 5: Aplicación estándar...");
        
        var deployment = new DeploymentPlan
        {
            PlanName = "Test Plan 5",
            DeployedTo = "Homologation",
            Status = "Running",
            Details = "StandardApp",
            ProcessedDetails = "StandardApp",
            LastUpdated = DateTime.Now
        };

        await notificationService.SendNotificationAsync(deployment);
        Console.WriteLine("✅ Notificación enviada");
        await Task.Delay(2000);
    }
} 