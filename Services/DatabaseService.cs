using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotifyOutSystems.Data;
using NotifyOutSystems.Models;

namespace NotifyOutSystems.Services;

public interface IDatabaseService
{
    Task<List<DeploymentPlan>> GetAllDeploymentPlansAsync();
    Task<DeploymentPlan?> GetDeploymentPlanByKeyAsync(string planName, string deployedTo);
    Task<DeploymentPlan> SaveDeploymentPlanAsync(DeploymentPlan plan);
    Task<DeploymentPlan> UpdateDeploymentPlanAsync(DeploymentPlan plan);
    Task<List<DeploymentPlan>> GetChangedDeploymentPlansAsync();
    Task MarkNotificationAsSentAsync(int planId);
    Task InitializeDatabaseAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeDatabaseAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("Base de datos inicializada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar la base de datos");
            throw;
        }
    }

    public async Task<List<DeploymentPlan>> GetAllDeploymentPlansAsync()
    {
        try
        {
            return await _context.DeploymentPlans
                .OrderByDescending(dp => dp.LastUpdated)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los planes de deployment");
            throw;
        }
    }

    public async Task<DeploymentPlan?> GetDeploymentPlanByKeyAsync(string planName, string deployedTo)
    {
        try
        {
            return await _context.DeploymentPlans
                .FirstOrDefaultAsync(dp => dp.PlanName == planName && dp.DeployedTo == deployedTo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el plan de deployment {PlanName} para {DeployedTo}", planName, deployedTo);
            throw;
        }
    }

    public async Task<DeploymentPlan> SaveDeploymentPlanAsync(DeploymentPlan plan)
    {
        try
        {
            var existingPlan = await GetDeploymentPlanByKeyAsync(plan.PlanName, plan.DeployedTo);
            
            if (existingPlan != null)
            {
                return await UpdateDeploymentPlanAsync(plan);
            }
            
            plan.FirstDetected = DateTime.Now;
            plan.LastUpdated = DateTime.Now;
            
            _context.DeploymentPlans.Add(plan);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Nuevo plan de deployment guardado: {PlanName} -> {DeployedTo}", plan.PlanName, plan.DeployedTo);
            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar el plan de deployment");
            throw;
        }
    }

    public async Task<DeploymentPlan> UpdateDeploymentPlanAsync(DeploymentPlan plan)
    {
        try
        {
            var existingPlan = await GetDeploymentPlanByKeyAsync(plan.PlanName, plan.DeployedTo);
            
            if (existingPlan == null)
            {
                return await SaveDeploymentPlanAsync(plan);
            }

            // Detectar cambios de estado
            var hasStatusChanged = existingPlan.Status != plan.Status;
            
            if (hasStatusChanged)
            {
                existingPlan.PreviousStatus = existingPlan.Status;
                existingPlan.HasStatusChanged = true;
                existingPlan.NotificationSent = false;
                
                // Calcular duración si cambió de Running a Finished
                if (existingPlan.IsRunning && plan.IsFinished)
                {
                    existingPlan.EndTime = DateTime.Now;
                    if (existingPlan.StartTime.HasValue)
                    {
                        existingPlan.Duration = existingPlan.EndTime.Value - existingPlan.StartTime.Value;
                    }
                }
                
                // Establecer tiempo de inicio si cambió a Running
                if (!existingPlan.IsRunning && plan.IsRunning)
                {
                    existingPlan.StartTime = DateTime.Now;
                }
                
                _logger.LogInformation("Cambio de estado detectado: {PlanName} -> {DeployedTo}: {OldStatus} -> {NewStatus}", 
                    plan.PlanName, plan.DeployedTo, existingPlan.Status, plan.Status);
            }

            // Actualizar propiedades
            existingPlan.Status = plan.Status;
            existingPlan.Details = plan.Details;
            existingPlan.ProcessedDetails = plan.ProcessedDetails;
            existingPlan.LastUpdated = DateTime.Now;
            existingPlan.Notes = plan.Notes;

            await _context.SaveChangesAsync();
            
            return existingPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el plan de deployment");
            throw;
        }
    }

    public async Task<List<DeploymentPlan>> GetChangedDeploymentPlansAsync()
    {
        try
        {
            return await _context.DeploymentPlans
                .Where(dp => dp.HasStatusChanged && !dp.NotificationSent)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los planes con cambios");
            throw;
        }
    }

    public async Task MarkNotificationAsSentAsync(int planId)
    {
        try
        {
            var plan = await _context.DeploymentPlans.FindAsync(planId);
            if (plan != null)
            {
                plan.NotificationSent = true;
                plan.HasStatusChanged = false;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar la notificación como enviada para el plan {PlanId}", planId);
            throw;
        }
    }
} 