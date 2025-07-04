using System.ComponentModel.DataAnnotations;

namespace NotifyOutSystems.Models;

public class DeploymentPlan
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string PlanName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string DeployedTo { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Status { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string ProcessedDetails { get; set; } = string.Empty;
    
    public DateTime? StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
    
    public TimeSpan? Duration { get; set; }
    
    public DateTime FirstDetected { get; set; }
    
    public DateTime LastUpdated { get; set; }
    
    [MaxLength(50)]
    public string PreviousStatus { get; set; } = string.Empty;
    
    public bool HasStatusChanged { get; set; }
    
    public bool NotificationSent { get; set; }
    
    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Propiedades calculadas
    public bool IsRunning => Status.Contains("Running", StringComparison.OrdinalIgnoreCase);
    
    public bool IsFinished => Status.Contains("Finished", StringComparison.OrdinalIgnoreCase);
    
    public bool IsHomologation => DeployedTo.Contains("Homologation", StringComparison.OrdinalIgnoreCase);
    
    public bool IsProduction => DeployedTo.Contains("Production", StringComparison.OrdinalIgnoreCase);
    
    public string GetNotificationMessage()
    {
        var environment = IsHomologation ? "Homologación" : 
                         IsProduction ? "Producción" : 
                         DeployedTo;
        
        var action = IsRunning ? "está haciendo" : "hizo";
        var duration = Duration.HasValue ? $" (Duración: {Duration.Value:hh\\:mm\\:ss})" : "";
        
        return $"{ProcessedDetails} {action} un pase a {environment}{duration}";
    }
} 