namespace ServerRoomMonitor.Models;

public class ServerRoom
{
public int Id { get; set; }


public string Name { get; set; } = "";

public string Location { get; set; } = "";

// Current operational status of the server room.
// Operational / Requires Fix / Awaiting Verification
public string Status { get; set; } = "Operational";

// Actual inspection results
public List<Inspection> Inspections { get; set; } = new();

// Inspections scheduled by administrators
public List<ScheduledInspection> ScheduledInspections { get; set; } = new();

}
