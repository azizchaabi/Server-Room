using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Data;

public class ApplicationDbContext : IdentityDbContext
{
public ApplicationDbContext(
DbContextOptions<ApplicationDbContext> options)
: base(options)
{
}


public DbSet<ServerRoom> ServerRooms => Set<ServerRoom>();

public DbSet<Inspection> Inspections => Set<Inspection>();

public DbSet<Reminder> Reminders => Set<Reminder>();

public DbSet<ScheduledInspection> ScheduledInspections =>
    Set<ScheduledInspection>();
public DbSet<PredictiveMaintenanceRecord> PredictiveMaintenanceRecords =>
    Set<PredictiveMaintenanceRecord>();
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // Inspection -> Technician
    builder.Entity<Inspection>()
        .HasOne(i => i.Technician)
        .WithMany()
        .HasForeignKey(i => i.TechnicianId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Entity<PredictiveMaintenanceRecord>()
    .Property(p => p.Temperature)
    .HasPrecision(5, 2);

    builder.Entity<PredictiveMaintenanceRecord>()
    .Property(p => p.TemperatureDeviation)
    .HasPrecision(5, 2);

    // Inspection -> ScheduledInspection
    builder.Entity<Inspection>()
        .HasOne(i => i.ScheduledInspection)
        .WithMany(s => s.Inspections)
        .HasForeignKey(i => i.ScheduledInspectionId)
        .OnDelete(DeleteBehavior.SetNull);

    // ScheduledInspection -> Technician
    builder.Entity<ScheduledInspection>()
        .HasOne(s => s.Technician)
        .WithMany()
        .HasForeignKey(s => s.TechnicianId)
        .OnDelete(DeleteBehavior.Restrict);

    // ScheduledInspection -> Admin who marked it fixed
    builder.Entity<ScheduledInspection>()
        .HasOne(s => s.FixedByAdmin)
        .WithMany()
        .HasForeignKey(s => s.FixedByAdminId)
        .OnDelete(DeleteBehavior.Restrict);

    // ScheduledInspection -> ServerRoom
    // Restrict prevents SQL Server multiple cascade paths.
    builder.Entity<ScheduledInspection>()
        .HasOne(s => s.ServerRoom)
        .WithMany(r => r.ScheduledInspections)
        .HasForeignKey(s => s.ServerRoomId)
        .OnDelete(DeleteBehavior.Restrict);

    // Inspection -> ServerRoom
    builder.Entity<Inspection>()
        .HasOne(i => i.ServerRoom)
        .WithMany(r => r.Inspections)
        .HasForeignKey(i => i.ServerRoomId)
        .OnDelete(DeleteBehavior.Cascade);

    // Inspection temperature precision
    builder.Entity<Inspection>()
        .Property(i => i.Temperature)
        .HasPrecision(5, 2);
}


}
