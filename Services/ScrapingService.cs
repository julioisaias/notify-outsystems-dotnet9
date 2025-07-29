using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NotifyOutSystems.Models;
using System.Text.RegularExpressions;

namespace NotifyOutSystems.Services;

public interface IScrapingService
{
    Task<List<DeploymentPlan>> ScrapeDeploymentPlansAsync();
    Task<bool> LoginAsync();
    Task<bool> IsLoggedInAsync();
    void Dispose();
}

public class ScrapingService : IScrapingService, IDisposable
{
    private readonly AppConfiguration _config;
    private readonly ILogger<ScrapingService> _logger;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private DateTime _lastLoginTime;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed = false;

    public ScrapingService(AppConfiguration config, ILogger<ScrapingService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<List<DeploymentPlan>> ScrapeDeploymentPlansAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!await EnsureAuthenticatedAsync())
            {
                _logger.LogError("No se pudo autenticar con OutSystems");
                return new List<DeploymentPlan>();
            }

            _logger.LogInformation("Iniciando scraping de planes de deployment");
            
            // Navegar a la página de staging list
            await _page!.GotoAsync(_config.StagingListUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Esperar a que aparezca la tabla
            await _page.WaitForSelectorAsync("table.table", new() { Timeout = 30000 });

            // Extraer datos de la tabla
            var deploymentPlans = await ExtractDeploymentPlansFromTableAsync();
            
            _logger.LogInformation("Scraping completado. Se encontraron {Count} planes de deployment", deploymentPlans.Count);
            
            return deploymentPlans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el scraping");
            return new List<DeploymentPlan>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> LoginAsync()
    {
        try
        {
            await InitializeBrowserAsync();
            
            _logger.LogInformation("Iniciando proceso de login");
            
            // Navegar a la página de login
            await _page!.GotoAsync(_config.LoginUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verificar si ya está logueado
            if (await IsLoggedInAsync())
            {
                _logger.LogInformation("Ya está logueado");
                return true;
            }

            // Buscar campos de login
            var usernameSelector = "input[type='text'], input[name*='user'], input[id*='user']";
            var passwordSelector = "input[type='password']";
            var loginButtonSelector = "input[type='submit'], button[type='submit'], .btn-primary";

            await _page.WaitForSelectorAsync(usernameSelector, new() { Timeout = 10000 });
            await _page.WaitForSelectorAsync(passwordSelector, new() { Timeout = 10000 });

            // Llenar credenciales
            await _page.FillAsync(usernameSelector, _config.Username);
            await _page.FillAsync(passwordSelector, _config.Password);

            // Hacer clic en login
            await _page.ClickAsync(loginButtonSelector);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verificar si el login fue exitoso
            var loginSuccess = await IsLoggedInAsync();
            if (loginSuccess)
            {
                _lastLoginTime = DateTime.Now;
                _logger.LogInformation("Login exitoso");
            }
            else
            {
                _logger.LogWarning("Login falló");
            }

            return loginSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el login");
            return false;
        }
    }

    public async Task<bool> IsLoggedInAsync()
    {
        try
        {
            if (_page == null) return false;

            // Verificar si estamos en una página que requiere autenticación
            var url = _page.Url;
            if (url.Contains("login", StringComparison.OrdinalIgnoreCase) || 
                url.Contains("signin", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Verificar si podemos acceder a la página de staging
            var response = await _page.GotoAsync(_config.StagingListUrl);
            if (response?.Status == 200 && !_page.Url.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EnsureAuthenticatedAsync()
    {
        // Verificar si necesitamos reautenticarnos
        if (_page == null || 
            DateTime.Now - _lastLoginTime > TimeSpan.FromMinutes(_config.SessionTimeoutMinutes) ||
            !await IsLoggedInAsync())
        {
            return await LoginAsync();
        }

        return true;
    }

    private async Task InitializeBrowserAsync()
    {
        if (_browser == null)
        {
            var playwright = await Playwright.CreateAsync();
            _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
            
            _context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36"
            });
            
            _page = await _context.NewPageAsync();
            
            _logger.LogInformation("Browser inicializado");
        }
    }

    private async Task<List<DeploymentPlan>> ExtractDeploymentPlansFromTableAsync()
    {
        var deploymentPlans = new List<DeploymentPlan>();

        try
        {
            // Buscar todas las filas de la tabla (excluyendo el header)
            var rows = await _page!.QuerySelectorAllAsync("table.table tr:not(.table-header)");
            
            foreach (var row in rows)
            {
                try
                {
                    var cells = await row.QuerySelectorAllAsync("td");
                    if (cells.Count >= 4)
                    {
                        var planName = await cells[0].TextContentAsync() ?? "";
                        var deployedTo = await cells[1].TextContentAsync() ?? "";
                        var status = await cells[2].TextContentAsync() ?? "";
                        var details = await cells[3].TextContentAsync() ?? "";

                        // Limpiar y procesar los datos
                        planName = planName.Trim();
                        deployedTo = deployedTo.Trim();
                        status = CleanStatus(status.Trim());
                        details = details.Trim();

                        // Procesar detalles según las reglas especificadas
                        var processedDetails = ProcessDetails(details);

                        if (!string.IsNullOrEmpty(planName) && !string.IsNullOrEmpty(deployedTo))
                        {
                            var deployment = new DeploymentPlan
                            {
                                PlanName = planName,
                                DeployedTo = deployedTo,
                                Status = status,
                                Details = details,
                                ProcessedDetails = processedDetails,
                                LastUpdated = DateTime.Now
                            };

                            deploymentPlans.Add(deployment);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al procesar una fila de la tabla");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer datos de la tabla");
        }

        return deploymentPlans;
    }

    private string ProcessDetails(string details)
    {
        if (string.IsNullOrEmpty(details))
            return details;

        // Dividir por espacios o comas
        var parts = details.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
            return details;

        // Si es una sola aplicación, retornar el nombre completo
        if (parts.Length == 1)
        {
            return parts[0];
        }

        // Si hay múltiples aplicaciones, indicar "Varias aplicaciones"
        return "Varias aplicaciones";
    }

    private string CleanStatus(string status)
    {
        if (string.IsNullOrEmpty(status))
            return status;

        // Extraer solo el estado, ignorando fechas y horas
        var statusParts = status.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (statusParts.Length > 0)
        {
            return statusParts[0].Trim();
        }

        return status;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _page?.CloseAsync().Wait();
            _context?.CloseAsync().Wait();
            _browser?.CloseAsync().Wait();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
} 