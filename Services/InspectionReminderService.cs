using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Services;

public class InspectionReminderService : BackgroundService
{
private readonly IServiceScopeFactory _scopeFactory;
private readonly ILogger<InspectionReminderService> _logger;


public InspectionReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<InspectionReminderService> logger)
{
    _scopeFactory = scopeFactory;
    _logger = logger;
}

protected override async Task ExecuteAsync(
    CancellationToken stoppingToken)
{
    _logger.LogInformation(
        "Inspection Reminder Service started.");

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            _logger.LogInformation(
                "Checking server rooms for overdue inspections...");

            await CheckForOverdueInspections(
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while checking for overdue inspections.");
        }

        // Check every minute.
        try
        {
            await Task.Delay(
                TimeSpan.FromMinutes(1),
                stoppingToken);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }

    _logger.LogInformation(
        "Inspection Reminder Service stopped.");
}

private async Task CheckForOverdueInspections(
    CancellationToken cancellationToken)
{
    using var scope = _scopeFactory.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var emailService = scope.ServiceProvider
        .GetRequiredService<EmailNotificationService>();

    var rooms = await context.ServerRooms
        .Include(r => r.Inspections)
        .ToListAsync(cancellationToken);

    _logger.LogInformation(
        "Found {RoomCount} server rooms to check.",
        rooms.Count);

    foreach (var room in rooms)
    {
        var lastInspection = room.Inspections
            .OrderByDescending(i => i.CheckedAt)
            .FirstOrDefault();

        bool overdue;

        // -----------------------------------------------------
        // DETERMINE WHETHER THE ROOM IS OVERDUE
        // -----------------------------------------------------

        if (lastInspection == null)
        {
            _logger.LogInformation(
                "Room {RoomId} ({RoomName}) has never been inspected.",
                room.Id,
                room.Name);

            overdue = true;
        }
        else
        {
            var daysSinceInspection =
                (DateTime.Now - lastInspection.CheckedAt).TotalDays;

            _logger.LogInformation(
                "Room {RoomId} ({RoomName}) - Last inspection: {CheckedAt}, Days since inspection: {Days:F2}.",
                room.Id,
                room.Name,
                lastInspection.CheckedAt,
                daysSinceInspection);

            overdue = daysSinceInspection >= 7;
        }

        // -----------------------------------------------------
        // ROOM IS NOT OVERDUE
        // -----------------------------------------------------

        if (!overdue)
        {
            _logger.LogInformation(
                "Room {RoomId} ({RoomName}) is not overdue.",
                room.Id,
                room.Name);

            continue;
        }

        // -----------------------------------------------------
        // ROOM IS OVERDUE
        // -----------------------------------------------------

        _logger.LogInformation(
            "Room {RoomId} ({RoomName}) IS OVERDUE.",
            room.Id,
            room.Name);

        // -----------------------------------------------------
        // CHECK FOR AN ACTIVE REMINDER
        //
        // IsRead is intentionally NOT used here.
        // A reminder being read does not mean the room
        // has been inspected.
        // -----------------------------------------------------

        var existingReminder = await context.Reminders
            .FirstOrDefaultAsync(
                r => r.ServerRoomId == room.Id &&
                     !r.IsRead,
                cancellationToken);

        if (existingReminder != null)
        {
            _logger.LogInformation(
                "Room {RoomId} ({RoomName}) already has an active reminder {ReminderId}. No duplicate reminder or email will be created.",
                room.Id,
                room.Name,
                existingReminder.Id);

            continue;
        }

        // -----------------------------------------------------
        // CREATE NEW REMINDER
        // -----------------------------------------------------

        var message = lastInspection == null
            ? $"{room.Name} has never been inspected."
            : $"{room.Name} has not been inspected for 7 days.";

        var reminder = new Reminder
        {
            ServerRoomId = room.Id,
            CreatedAt = DateTime.Now,
            Message = message,
            IsRead = false
        };

        context.Reminders.Add(reminder);

        await context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Created inspection reminder for server room {RoomName} (ID: {RoomId}).",
            room.Name,
            room.Id);

        // -----------------------------------------------------
        // SEND EMAIL
        //
        // The email is sent only when a NEW reminder is created.
        // This prevents the background service from sending an
        // email every minute while the room remains overdue.
        // -----------------------------------------------------

        _logger.LogInformation(
            "Sending overdue inspection email for room {RoomName} (ID: {RoomId})...",
            room.Name,
            room.Id);

        try
        {
            await emailService.SendInspectionReminderAsync(
                room.Name,
                room.Id,
                message);

            _logger.LogInformation(
                "Finished sending overdue inspection email for room {RoomName}.",
                room.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send overdue inspection email for room {RoomName} (ID: {RoomId}).",
                room.Name,
                room.Id);
        }
    }
}


}
