using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using MimeKit;

namespace ServerRoomMonitor.Services;

public class EmailNotificationService
{
private readonly IConfiguration _configuration;
private readonly UserManager<IdentityUser> _userManager;
private readonly ILogger<EmailNotificationService> _logger;


public EmailNotificationService(
    IConfiguration configuration,
    UserManager<IdentityUser> userManager,
    ILogger<EmailNotificationService> logger)
{
    _configuration = configuration;
    _userManager = userManager;
    _logger = logger;
}

// =========================================================
// SCHEDULED INSPECTION
// Sends only to the assigned technician.
// =========================================================

public async Task SendScheduledInspectionAsync(
    string technicianEmail,
    string roomName,
    int roomId,
    DateTime scheduledAt,
    DateTime? deadline,
    string? notes)
{
    if (string.IsNullOrWhiteSpace(technicianEmail))
    {
        _logger.LogWarning(
            "Cannot send scheduled inspection email because the technician has no email address.");

        return;
    }

    var subject = $"Inspection Scheduled - {roomName}";

    var baseUrl = GetApplicationUrl();

    var inspectionUrl =
        $"{baseUrl}/Technician/Index";

    var deadlineText = deadline.HasValue
        ? deadline.Value.ToString("dd/MM/yyyy HH:mm")
        : "No deadline specified";

    var notesText = string.IsNullOrWhiteSpace(notes)
        ? "No additional notes."
        : notes;

    var plainTextContent =
        $"""
        SERVER ROOM MONITOR
        INSPECTION SCHEDULED

        You have been assigned a server room inspection.

        Server Room
        {roomName}

        Scheduled Date
        {scheduledAt:dd/MM/yyyy HH:mm}

        Deadline
        {deadlineText}

        Notes
        {notesText}

        Open Technician Dashboard:
        {inspectionUrl}

        Please complete the inspection before the deadline.

        This is an automated notification from
        Server Room Monitor.
        """;

    var htmlContent =
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>Inspection Scheduled - {{roomName}}</title>

            <style>
                body {
                    margin: 0;
                    padding: 0;
                    background-color: #f3f4f6;
                    font-family: Arial, Helvetica, sans-serif;
                    color: #1f2937;
                }

                .wrapper {
                    width: 100%;
                    padding: 40px 15px;
                    box-sizing: border-box;
                }

                .container {
                    max-width: 620px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border-radius: 12px;
                    overflow: hidden;
                    border: 1px solid #e5e7eb;
                }

                .header {
                    background-color: #2563eb;
                    color: #ffffff;
                    padding: 28px 32px;
                }

                .header-title {
                    margin: 0;
                    font-size: 24px;
                    font-weight: 600;
                }

                .header-subtitle {
                    margin: 7px 0 0;
                    font-size: 14px;
                    opacity: 0.9;
                }

                .content {
                    padding: 32px;
                }

                .intro {
                    margin: 0 0 24px;
                    font-size: 15px;
                    line-height: 1.6;
                    color: #4b5563;
                }

                .room-card {
                    background-color: #eff6ff;
                    border: 1px solid #bfdbfe;
                    border-left: 5px solid #2563eb;
                    border-radius: 8px;
                    padding: 18px 20px;
                    margin-bottom: 26px;
                }

                .room-label {
                    font-size: 11px;
                    font-weight: 700;
                    text-transform: uppercase;
                    letter-spacing: 0.7px;
                    color: #1d4ed8;
                    margin-bottom: 6px;
                }

                .room-name {
                    font-size: 21px;
                    font-weight: 600;
                    color: #111827;
                }

                .details {
                    background-color: #f9fafb;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                    padding: 20px;
                }

                .detail {
                    margin-bottom: 14px;
                }

                .detail:last-child {
                    margin-bottom: 0;
                }

                .label {
                    font-size: 11px;
                    font-weight: 700;
                    text-transform: uppercase;
                    color: #6b7280;
                    margin-bottom: 4px;
                }

                .value {
                    font-size: 15px;
                    color: #111827;
                }

                .notes {
                    white-space: pre-line;
                }

                .button-container {
                    text-align: center;
                    margin: 30px 0 10px;
                }

                .button {
                    display: inline-block;
                    background-color: #2563eb;
                    color: #ffffff !important;
                    text-decoration: none;
                    font-weight: 600;
                    font-size: 15px;
                    padding: 13px 26px;
                    border-radius: 7px;
                }

                .footer {
                    padding: 20px 30px;
                    background-color: #f9fafb;
                    border-top: 1px solid #e5e7eb;
                    color: #6b7280;
                    font-size: 12px;
                    text-align: center;
                    line-height: 1.5;
                }
            </style>
        </head>

        <body>
            <div class="wrapper">
                <div class="container">

                    <div class="header">
                        <h1 class="header-title">
                            📅 Inspection Scheduled
                        </h1>

                        <p class="header-subtitle">
                            Server Room Monitor
                        </p>
                    </div>

                    <div class="content">

                        <p class="intro">
                            You have been assigned a new server room
                            inspection.
                        </p>

                        <div class="room-card">
                            <div class="room-label">
                                Server Room
                            </div>

                            <div class="room-name">
                                {{roomName}}
                            </div>
                        </div>

                        <div class="details">

                            <div class="detail">
                                <div class="label">
                                    Scheduled Date
                                </div>

                                <div class="value">
                                    {{scheduledAt:dd/MM/yyyy HH:mm}}
                                </div>
                            </div>

                            <div class="detail">
                                <div class="label">
                                    Deadline
                                </div>

                                <div class="value">
                                    {{deadlineText}}
                                </div>
                            </div>

                            <div class="detail">
                                <div class="label">
                                    Notes
                                </div>

                                <div class="value notes">
                                    {{notesText}}
                                </div>
                            </div>

                        </div>

                        <div class="button-container">
                            <a href="{{inspectionUrl}}"
                               class="button">
                                Open Technician Dashboard
                            </a>
                        </div>

                    </div>

                    <div class="footer">
                        This is an automated notification from
                        Server Room Monitor.
                        <br>
                        Please do not reply to this email.
                    </div>

                </div>
            </div>
        </body>
        </html>
        """;

    await SendEmailAsync(
        technicianEmail,
        subject,
        plainTextContent,
        htmlContent);
}

// =========================================================
// THIRD FAILED ATTEMPT
// Sends only to Admin users.
// =========================================================

public async Task SendInspectionFailureAsync(
    string roomName,
    int roomId,
    string message)
{
    var admins = await GetUsersInRoleWithEmailAsync("Admin");

    if (!admins.Any())
    {
        _logger.LogWarning(
            "No Admin users with email addresses were found.");

        return;
    }

    var subject =
        $"Inspection Failed - 3 Attempts - {roomName}";

    var baseUrl = GetApplicationUrl();

    var roomUrl =
        $"{baseUrl}/ServerRooms/Details?id={roomId}";

    var plainTextContent =
        $"""
        SERVER ROOM MONITOR
        INSPECTION REQUIRES ATTENTION

        The inspection for the following server room
        has failed after 3 attempts.

        Server Room
        {roomName}

        {message}

        The room now requires administrative attention.

        View Server Room:
        {roomUrl}

        Please review the inspection results and take
        the necessary action.

        This is an automated notification from
        Server Room Monitor.
        """;

    var htmlContent =
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>Inspection Failed - {{roomName}}</title>

            <style>
                body {
                    margin: 0;
                    padding: 0;
                    background-color: #f3f4f6;
                    font-family: Arial, Helvetica, sans-serif;
                    color: #1f2937;
                }

                .wrapper {
                    width: 100%;
                    padding: 40px 15px;
                    box-sizing: border-box;
                }

                .container {
                    max-width: 620px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border-radius: 12px;
                    overflow: hidden;
                    border: 1px solid #e5e7eb;
                }

                .header {
                    background-color: #dc3545;
                    color: #ffffff;
                    padding: 28px 32px;
                }

                .header-title {
                    margin: 0;
                    font-size: 24px;
                    font-weight: 600;
                }

                .header-subtitle {
                    margin: 7px 0 0;
                    font-size: 14px;
                    opacity: 0.9;
                }

                .content {
                    padding: 32px;
                }

                .intro {
                    margin: 0 0 24px;
                    font-size: 15px;
                    line-height: 1.6;
                    color: #4b5563;
                }

                .room-card {
                    background-color: #fff5f5;
                    border: 1px solid #fecaca;
                    border-left: 5px solid #dc3545;
                    border-radius: 8px;
                    padding: 18px 20px;
                    margin-bottom: 26px;
                }

                .room-label {
                    font-size: 11px;
                    font-weight: 700;
                    text-transform: uppercase;
                    color: #991b1b;
                    margin-bottom: 6px;
                }

                .room-name {
                    font-size: 21px;
                    font-weight: 600;
                    color: #111827;
                }

                .details {
                    background-color: #f9fafb;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                    padding: 20px;
                }

                .message {
                    white-space: pre-line;
                    font-size: 14px;
                    line-height: 1.7;
                    color: #374151;
                }

                .action-box {
                    margin-top: 26px;
                    background-color: #fef2f2;
                    border: 1px solid #fecaca;
                    border-radius: 8px;
                    padding: 16px 18px;
                    font-size: 14px;
                    line-height: 1.5;
                    color: #7f1d1d;
                }

                .button-container {
                    text-align: center;
                    margin: 30px 0 10px;
                }

                .button {
                    display: inline-block;
                    background-color: #dc3545;
                    color: #ffffff !important;
                    text-decoration: none;
                    font-weight: 600;
                    font-size: 15px;
                    padding: 13px 26px;
                    border-radius: 7px;
                }

                .footer {
                    padding: 20px 30px;
                    background-color: #f9fafb;
                    border-top: 1px solid #e5e7eb;
                    color: #6b7280;
                    font-size: 12px;
                    text-align: center;
                    line-height: 1.5;
                }
            </style>
        </head>

        <body>
            <div class="wrapper">
                <div class="container">

                    <div class="header">
                        <h1 class="header-title">
                            ⚠ Inspection Requires Attention
                        </h1>

                        <p class="header-subtitle">
                            Server Room Monitor
                        </p>
                    </div>

                    <div class="content">

                        <p class="intro">
                            This inspection has failed after
                            three attempts and now requires
                            administrative attention.
                        </p>

                        <div class="room-card">
                            <div class="room-label">
                                Server Room
                            </div>

                            <div class="room-name">
                                {{roomName}}
                            </div>
                        </div>

                        <div class="details">
                            <div class="message">
                                {{message}}
                            </div>
                        </div>

                        <div class="action-box">
                            <strong>Action required</strong>
                            <br>
                            Please review the inspection results
                            and take the necessary action.
                        </div>

                        <div class="button-container">
                            <a href="{{roomUrl}}"
                               class="button">
                                View Server Room
                            </a>
                        </div>

                    </div>

                    <div class="footer">
                        This is an automated notification from
                        Server Room Monitor.
                        <br>
                        Please do not reply to this email.
                    </div>

                </div>
            </div>
        </body>
        </html>
        """;

    foreach (var admin in admins)
    {
        await SendEmailAsync(
            admin.Email!,
            subject,
            plainTextContent,
            htmlContent);
    }
}

// =========================================================
// 7-DAY OVERDUE REMINDER
// Sends ONLY to Admin users.
// =========================================================

public async Task SendInspectionReminderAsync(
    string roomName,
    int roomId,
    string message)
{
    var admins = await GetUsersInRoleWithEmailAsync("Admin");

    if (!admins.Any())
    {
        _logger.LogWarning(
            "No Admin users with email addresses were found.");

        return;
    }

    var subject =
        $"Inspection Reminder - {roomName}";

    var baseUrl = GetApplicationUrl();

    var roomUrl =
        $"{baseUrl}/ServerRooms/Details?id={roomId}";

    var plainTextContent =
        $"""
        SERVER ROOM MONITOR
        INSPECTION REMINDER

        Server Room
        {roomName}

        This server room is overdue for inspection.

        {message}

        View Server Room:
        {roomUrl}

        Please review this room and assign a technician
        for inspection.

        This is an automated notification from
        Server Room Monitor.
        """;

    var htmlContent =
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
            <meta name="viewport"
                  content="width=device-width, initial-scale=1.0">

            <title>Inspection Reminder - {{roomName}}</title>

            <style>
                body {
                    margin: 0;
                    padding: 0;
                    background-color: #f3f4f6;
                    font-family: Arial, Helvetica, sans-serif;
                    color: #1f2937;
                }

                .wrapper {
                    width: 100%;
                    padding: 40px 15px;
                    box-sizing: border-box;
                }

                .container {
                    max-width: 620px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    border-radius: 12px;
                    overflow: hidden;
                    border: 1px solid #e5e7eb;
                }

                .header {
                    background-color: #d99a00;
                    color: #ffffff;
                    padding: 28px 32px;
                }

                .header-title {
                    margin: 0;
                    font-size: 24px;
                    font-weight: 600;
                }

                .header-subtitle {
                    margin: 7px 0 0;
                    font-size: 14px;
                    opacity: 0.9;
                }

                .content {
                    padding: 32px;
                }

                .intro {
                    margin: 0 0 24px;
                    font-size: 15px;
                    line-height: 1.6;
                    color: #4b5563;
                }

                .room-card {
                    background-color: #fffbeb;
                    border: 1px solid #fde68a;
                    border-left: 5px solid #d99a00;
                    border-radius: 8px;
                    padding: 18px 20px;
                    margin-bottom: 26px;
                }

                .room-label {
                    font-size: 11px;
                    font-weight: 700;
                    text-transform: uppercase;
                    color: #92400e;
                    margin-bottom: 6px;
                }

                .room-name {
                    font-size: 21px;
                    font-weight: 600;
                    color: #111827;
                }

                .details {
                    background-color: #f9fafb;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                    padding: 20px;
                }

                .message {
                    white-space: pre-line;
                    font-size: 14px;
                    line-height: 1.7;
                    color: #374151;
                }

                .action-box {
                    margin-top: 26px;
                    background-color: #fffbeb;
                    border: 1px solid #fde68a;
                    border-radius: 8px;
                    padding: 16px 18px;
                    font-size: 14px;
                    line-height: 1.5;
                    color: #78350f;
                }

                .button-container {
                    text-align: center;
                    margin: 30px 0 10px;
                }

                .button {
                    display: inline-block;
                    background-color: #d99a00;
                    color: #ffffff !important;
                    text-decoration: none;
                    font-weight: 600;
                    font-size: 15px;
                    padding: 13px 26px;
                    border-radius: 7px;
                }

                .footer {
                    padding: 20px 30px;
                    background-color: #f9fafb;
                    border-top: 1px solid #e5e7eb;
                    color: #6b7280;
                    font-size: 12px;
                    text-align: center;
                    line-height: 1.5;
                }
            </style>
        </head>

        <body>
            <div class="wrapper">
                <div class="container">

                    <div class="header">
                        <h1 class="header-title">
                            ⏰ Inspection Reminder
                        </h1>

                        <p class="header-subtitle">
                            Server Room Monitor
                        </p>
                    </div>

                    <div class="content">

                        <p class="intro">
                            This server room has not been inspected
                            for seven days and requires attention.
                        </p>

                        <div class="room-card">
                            <div class="room-label">
                                Server Room
                            </div>

                            <div class="room-name">
                                {{roomName}}
                            </div>
                        </div>

                        <div class="details">
                            <div class="message">
                                {{message}}
                            </div>
                        </div>

                        <div class="action-box">
                            <strong>Action required</strong>
                            <br>
                            Please review this room and assign a
                            technician for inspection.
                        </div>

                        <div class="button-container">
                            <a href="{{roomUrl}}"
                               class="button">
                                View Server Room
                            </a>
                        </div>

                    </div>

                    <div class="footer">
                        This is an automated notification from
                        Server Room Monitor.
                        <br>
                        Please do not reply to this email.
                    </div>

                </div>
            </div>
        </body>
        </html>
        """;

    foreach (var admin in admins)
    {
        await SendEmailAsync(
            admin.Email!,
            subject,
            plainTextContent,
            htmlContent);
    }
}

// =========================================================
// HELPER: GET USERS IN A ROLE WITH EMAIL
// =========================================================

private async Task<List<IdentityUser>> GetUsersInRoleWithEmailAsync(
    string role)
{
    var users = _userManager.Users.ToList();

    var result = new List<IdentityUser>();

    foreach (var user in users)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            continue;

        if (await _userManager.IsInRoleAsync(user, role))
        {
            result.Add(user);
        }
    }

    return result;
}

// =========================================================
// HELPER: SEND EMAIL
// =========================================================

private async Task SendEmailAsync(
    string recipientEmail,
    string subject,
    string plainTextContent,
    string htmlContent)
{
    var host = _configuration["EmailSettings:Host"];
    var portString = _configuration["EmailSettings:Port"];
    var username = _configuration["EmailSettings:Username"];
    var password = _configuration["EmailSettings:Password"];
    var fromEmail = _configuration["EmailSettings:FromEmail"];

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(portString) ||
        string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password) ||
        string.IsNullOrWhiteSpace(fromEmail))
    {
        _logger.LogError(
            "Email SMTP settings are not configured correctly.");

        return;
    }

    if (!int.TryParse(portString, out var port))
    {
        _logger.LogError(
            "Email SMTP port is invalid: {Port}",
            portString);

        return;
    }

    try
    {
        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            host,
            port,
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            username,
            password);

        var email = new MimeMessage();

        email.From.Add(
            new MailboxAddress(
                "Server Room Monitor",
                fromEmail));

        email.To.Add(
            new MailboxAddress(
                recipientEmail,
                recipientEmail));

        email.Subject = subject;

        var body = new BodyBuilder
        {
            TextBody = plainTextContent,
            HtmlBody = htmlContent
        };

        email.Body = body.ToMessageBody();

        await smtp.SendAsync(email);

        await smtp.DisconnectAsync(true);

        _logger.LogInformation(
            "Email sent successfully to {Email}. Subject: {Subject}",
            recipientEmail,
            subject);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to send email to {Email}. Subject: {Subject}",
            recipientEmail,
            subject);
    }
}

// =========================================================
// APPLICATION URL
// =========================================================

private string GetApplicationUrl()
{
    var baseUrl = _configuration["ApplicationUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        baseUrl = "http://localhost:5263";
    }

    return baseUrl.TrimEnd('/');
}


}
