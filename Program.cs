using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotifyOutSystems.Data;
using NotifyOutSystems.Models;
using NotifyOutSystems.Services;
using Serilog;
using System.Reflection;

namespace NotifyOutSystems;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configurar Serilog
        var configuration = GetConfiguration();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        try
        {
            Log.Information("=== OutSystems Monitor Iniciando ===");
            Log.Information("Versión: {Version}", Assembly.GetExecutingAssembly().GetName().Version);
            Log.Information("Plataforma: {Platform}", Environment.OSVersion);
            
            // Instalar Playwright browsers si es necesario
            InstallPlaywrightBrowsersAsync();
            
            // Crear y ejecutar el host
            var host = CreateHostBuilder(args).Build();
            
            // Configurar manejo de señales del sistema
            Console.CancelKeyPress += (_, e) =>
            {
                Log.Information("Recibida señal de interrupción. Cerrando aplicación...");
                e.Cancel = true;
                host.StopAsync().Wait();
            };
            
            Log.Information("Aplicación iniciada. Presiona Ctrl+C para detener.");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error crítico al iniciar la aplicación");
            throw;
        }
        finally
        {
            Log.Information("=== OutSystems Monitor Finalizando ===");
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Configuración
                var config = context.Configuration;
                var appConfig = new AppConfiguration();
                config.GetSection("OutSystemsSettings").Bind(appConfig);
                
                // Validar configuración
                ValidateConfiguration(appConfig);
                
                services.AddSingleton(appConfig);
                
                // Entity Framework
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(config.GetConnectionString("DefaultConnection")));
                
                // Servicios
                services.AddScoped<IDatabaseService, DatabaseService>();
                services.AddScoped<IScrapingService, ScrapingService>();
                services.AddScoped<INotificationService, NotificationService>();
                services.AddSingleton<IMonitoringService, MonitoringService>();
                
                // Registrar el servicio de monitoreo como hosted service
                services.AddHostedService<MonitoringService>();
                
                // Logging
                services.AddLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog(dispose: true);
                });
            });

    private static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static void ValidateConfiguration(AppConfiguration config)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(config.LoginUrl))
            errors.Add("LoginUrl es requerido");
        
        if (string.IsNullOrWhiteSpace(config.StagingListUrl))
            errors.Add("StagingListUrl es requerido");
        
        if (string.IsNullOrWhiteSpace(config.Username))
            errors.Add("Username es requerido");
        
        if (string.IsNullOrWhiteSpace(config.Password))
            errors.Add("Password es requerido");
        
        if (config.MonitoringIntervalSeconds < 1)
            errors.Add("MonitoringIntervalSeconds debe ser mayor a 0");
        
        if (errors.Count > 0)
        {
            var errorMessage = $"Errores de configuración:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}";
            Log.Fatal(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        
        Log.Information("Configuración validada correctamente");
        Log.Information("LoginUrl: {LoginUrl}", config.LoginUrl);
        Log.Information("StagingListUrl: {StagingListUrl}", config.StagingListUrl);
        Log.Information("Username: {Username}", config.Username);
        Log.Information("MonitoringInterval: {Seconds} segundos", config.MonitoringIntervalSeconds);
        Log.Information("Notificaciones: {Enabled}", config.EnableNotifications ? "Habilitadas" : "Deshabilitadas");
    }

    private static void InstallPlaywrightBrowsersAsync()
    {
        try
        {
            Log.Information("Verificando instalación de Playwright browsers...");
            
            // Verificar si ya está instalado
            var playwrightPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                ".cache", "ms-playwright");
            
            if (!Directory.Exists(playwrightPath))
            {
                Log.Information("Instalando Playwright browsers...");
                
                // Instalar browsers automáticamente
                Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                
                Log.Information("Playwright browsers instalados correctamente");
            }
            else
            {
                Log.Information("Playwright browsers ya están instalados");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error al verificar/instalar Playwright browsers. La aplicación continuará...");
        }
    }
} 