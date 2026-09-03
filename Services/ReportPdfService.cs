using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ServerRoomMonitor.Services;

public class ReportPdfService
{
public byte[] GenerateReport(
int totalRooms,
int totalInspections,
int successfulInspections,
int failedInspections,
int overdueRooms,
double complianceRate,
List<RoomReportData> roomReports,
List<RecentInspectionData> recentInspections)
{
QuestPDF.Settings.License = LicenseType.Community;


    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);

            page.DefaultTextStyle(x =>
                x.FontSize(10));

            // Header
            page.Header()
                .Column(column =>
                {
                    column.Item()
                        .Text("SERVER ROOM MONITOR")
                        .FontSize(22)
                        .Bold();

                    column.Item()
                        .Text("Server Room Monitoring Report")
                        .FontSize(14)
                        .FontColor(Colors.Grey.Darken1);

                    column.Item()
                        .PaddingTop(10)
                        .Text(
                            $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    column.Item()
                        .PaddingTop(10)
                        .LineHorizontal(1);
                });

            // Content
            page.Content()
                .PaddingVertical(20)
                .Column(column =>
                {
                    column.Spacing(15);

                    // Summary
                    column.Item()
                        .Text("Summary")
                        .FontSize(16)
                        .Bold();

                    column.Item()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            AddSummaryCell(
                                table,
                                "Server Rooms",
                                totalRooms.ToString());

                            AddSummaryCell(
                                table,
                                "Inspections",
                                totalInspections.ToString());

                            AddSummaryCell(
                                table,
                                "Passed",
                                successfulInspections.ToString(),
                                Colors.Green.Darken2);

                            AddSummaryCell(
                                table,
                                "Failed",
                                failedInspections.ToString(),
                                Colors.Red.Darken2);

                            AddSummaryCell(
                                table,
                                "Overdue",
                                overdueRooms.ToString(),
                                overdueRooms > 0
                                    ? Colors.Orange.Darken2
                                    : Colors.Green.Darken2);

                            AddSummaryCell(
                                table,
                                "Compliance",
                                $"{complianceRate:F1}%");
                        });

                    // Server room status
                    column.Item()
                        .Text("Server Room Status")
                        .FontSize(16)
                        .Bold();

                    if (roomReports.Any())
                    {
                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.3f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Server Room")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Location")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Last Inspection")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Status")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Inspections")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Failures")
                                        .Bold()
                                        .FontColor(Colors.White);
                                });

                                foreach (var room in roomReports)
                                {
                                    var status =
                                        !room.LastInspection.HasValue
                                            ? "Never inspected"
                                            : room.DaysSinceInspection >= 7
                                                ? "Overdue"
                                                : "Up to date";

                                    var statusColor =
                                        status == "Up to date"
                                            ? Colors.Green.Darken2
                                            : Colors.Red.Darken2;

                                    AddTableCell(
                                        table,
                                        room.Name);

                                    AddTableCell(
                                        table,
                                        room.Location);

                                    AddTableCell(
                                        table,
                                        room.LastInspection.HasValue
                                            ? room.LastInspection.Value
                                                .ToString("dd/MM/yyyy HH:mm")
                                            : "Never");

                                    AddTableCell(
                                        table,
                                        status,
                                        statusColor);

                                    AddTableCell(
                                        table,
                                        room.TotalInspections.ToString());

                                    AddTableCell(
                                        table,
                                        room.FailedInspections.ToString(),
                                        room.FailedInspections > 0
                                            ? Colors.Red.Darken2
                                            : Colors.Green.Darken2);
                                }
                            });
                    }
                    else
                    {
                        column.Item()
                            .Text("No server rooms found.")
                            .FontColor(Colors.Grey.Darken1);
                    }

                    // Recent inspections
                    column.Item()
                        .Text("Recent Inspections")
                        .FontSize(16)
                        .Bold();

                    if (recentInspections.Any())
                    {
                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Date")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Server Room")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Technician")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Temperature")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(6)
                                        .Text("Result")
                                        .Bold()
                                        .FontColor(Colors.White);
                                });

                                foreach (var inspection in recentInspections)
                                {
                                    AddTableCell(
                                        table,
                                        inspection.CheckedAt
                                            .ToString("dd/MM/yyyy HH:mm"));

                                    AddTableCell(
                                        table,
                                        inspection.RoomName);

                                    AddTableCell(
                                        table,
                                        inspection.TechnicianEmail);

                                    AddTableCell(
                                        table,
                                        $"{inspection.Temperature} °C");

                                    AddTableCell(
                                        table,
                                        inspection.IsOk
                                            ? "OK"
                                            : "Attention required",
                                        inspection.IsOk
                                            ? Colors.Green.Darken2
                                            : Colors.Red.Darken2);
                                }
                            });
                    }
                    else
                    {
                        column.Item()
                            .Text(
                                "No inspections have been recorded.")
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

            // Footer
            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span(
                        "Server Room Monitor - Monitoring Report | ");

                    text.CurrentPageNumber();
                });
        });
    }).GeneratePdf();
}

private static void AddSummaryCell(
    TableDescriptor table,
    string label,
    string value,
    string? valueColor = null)
{
    table.Cell()
        .Background(Colors.Grey.Lighten4)
        .Border(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(10)
        .Column(column =>
        {
            column.Item()
                .Text(label)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);

            if (valueColor != null)
            {
                column.Item()
                    .PaddingTop(3)
                    .Text(value)
                    .FontSize(16)
                    .Bold()
                    .FontColor(valueColor);
            }
            else
            {
                column.Item()
                    .PaddingTop(3)
                    .Text(value)
                    .FontSize(16)
                    .Bold();
            }
        });
}

private static void AddTableCell(
    TableDescriptor table,
    string text,
    string? textColor = null)
{
    var cell = table.Cell()
        .BorderBottom(1)
        .BorderColor(Colors.Grey.Lighten2)
        .Padding(6)
        .Text(text)
        .FontColor(
            textColor ?? Colors.Grey.Darken4);
    
    if (textColor != null)
        cell.Bold();
}


}

public class RoomReportData
{
public int Id { get; set; }


public string Name { get; set; } = "";

public string Location { get; set; } = "";

public DateTime? LastInspection { get; set; }

public double DaysSinceInspection { get; set; }

public int TotalInspections { get; set; }

public int FailedInspections { get; set; }


}

public class RecentInspectionData
{
public int Id { get; set; }


public DateTime CheckedAt { get; set; }

public string RoomName { get; set; } = "";

public string TechnicianEmail { get; set; } = "";

public decimal Temperature { get; set; }

public bool IsOk { get; set; }


}
