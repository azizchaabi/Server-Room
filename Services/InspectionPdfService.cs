using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ServerRoomMonitor.Models;

namespace ServerRoomMonitor.Services;

public class InspectionPdfService
{
    public byte[] GenerateInspectionReport(Inspection inspection)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var roomName = inspection.ServerRoom?.Name ?? "Unknown";
        var location = inspection.ServerRoom?.Location ?? "Unknown";
        var technician = inspection.Technician?.UserName
            ?? inspection.Technician?.Email
            ?? "Not recorded";

        var resultText = inspection.IsOk
            ? "INSPECTION OK"
            : "ATTENTION REQUIRED";

        var resultColor = inspection.IsOk
            ? Colors.Green.Darken2
            : Colors.Red.Darken2;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.DefaultTextStyle(x =>
                    x.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("SERVER ROOM MONITOR")
                            .FontSize(22)
                            .Bold();

                        column.Item()
                            .Text("Inspection Report")
                            .FontSize(14)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(10)
                            .LineHorizontal(1);
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Server room information
                        column.Item()
                            .Text("Server Room")
                            .FontSize(16)
                            .Bold();

                        column.Item()
                            .Background(Colors.Grey.Lighten4)
                            .Padding(15)
                            .Column(info =>
                            {
                                info.Spacing(5);

                                info.Item()
                                    .Text($"Name: {roomName}");

                                info.Item()
                                    .Text($"Location: {location}");

                                info.Item()
                                    .Text(
                                        $"Inspection date: {inspection.CheckedAt:dd/MM/yyyy HH:mm}");

                                info.Item()
                                    .Text($"Technician: {technician}");
                            });

                        // Overall result
                        column.Item()
                            .PaddingTop(5)
                            .Background(resultColor)
                            .Padding(12)
                            .AlignCenter()
                            .Text(resultText)
                            .FontSize(15)
                            .Bold()
                            .FontColor(Colors.White);

                        // Inspection checks
                        column.Item()
                            .Text("Inspection Checks")
                            .FontSize(16)
                            .Bold();

                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(8)
                                        .Text("Check")
                                        .Bold()
                                        .FontColor(Colors.White);

                                    header.Cell()
                                        .Background(Colors.Grey.Darken2)
                                        .Padding(8)
                                        .Text("Result")
                                        .Bold()
                                        .FontColor(Colors.White);
                                });

                                AddCheck(
                                    table,
                                    "Temperature",
                                    $"{inspection.Temperature} °C",
                                    inspection.TemperatureOk);

                                AddCheck(
                                    table,
                                    "Air Conditioning",
                                    inspection.AirConditioningOk
                                        ? "OK"
                                        : "Not OK",
                                    inspection.AirConditioningOk);

                                AddCheck(
                                    table,
                                    "Overheating Alarm",
                                    inspection.NoOverheatingAlarm
                                        ? "No Alarm"
                                        : "Alarm Detected",
                                    inspection.NoOverheatingAlarm);

                                AddCheck(
                                    table,
                                    "Water Leak",
                                    inspection.NoWaterLeak
                                        ? "No Leak"
                                        : "Leak Detected",
                                    inspection.NoWaterLeak);

                                AddCheck(
                                    table,
                                    "Power",
                                    inspection.PowerOk
                                        ? "OK"
                                        : "Not OK",
                                    inspection.PowerOk);

                                AddCheck(
                                    table,
                                    "Room Cleanliness",
                                    inspection.RoomClean
                                        ? "Clean"
                                        : "Needs Attention",
                                    inspection.RoomClean);
                            });

                        // Technician notes
                        column.Item()
                            .PaddingTop(5)
                            .Text("Technician Notes")
                            .FontSize(16)
                            .Bold();

                        column.Item()
                            .Background(Colors.Grey.Lighten4)
                            .Padding(15)
                            .Text(
                                string.IsNullOrWhiteSpace(inspection.Notes)
                                    ? "No notes were added for this inspection."
                                    : inspection.Notes);

                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span(
                            "Server Room Monitor - Inspection Report | ");

                        text.CurrentPageNumber();
                    });
            });
        }).GeneratePdf();
    }

    private static void AddCheck(
        TableDescriptor table,
        string check,
        string result,
        bool isOk)
    {
        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Text(check);

        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Text(result)
            .FontColor(
                isOk
                    ? Colors.Green.Darken2
                    : Colors.Red.Darken2)
            .Bold();
    }
}