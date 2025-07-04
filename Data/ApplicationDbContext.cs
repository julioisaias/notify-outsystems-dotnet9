using Microsoft.EntityFrameworkCore;
using NotifyOutSystems.Models;

namespace NotifyOutSystems.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DeploymentPlan> DeploymentPlans { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de DeploymentPlan
        modelBuilder.Entity<DeploymentPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.PlanName)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.DeployedTo)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Details)
                .HasMaxLength(500);
            
            entity.Property(e => e.ProcessedDetails)
                .HasMaxLength(200);
            
            entity.Property(e => e.PreviousStatus)
                .HasMaxLength(50);
            
            entity.Property(e => e.Notes)
                .HasMaxLength(1000);
            
            entity.Property(e => e.FirstDetected)
                .IsRequired();
            
            entity.Property(e => e.LastUpdated)
                .IsRequired();
            
            // Índices para mejorar el rendimiento
            entity.HasIndex(e => e.PlanName);
            entity.HasIndex(e => e.DeployedTo);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastUpdated);
            entity.HasIndex(e => new { e.PlanName, e.DeployedTo }).IsUnique();
        });
    }
} 