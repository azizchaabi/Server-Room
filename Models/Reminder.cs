namespace ServerRoomMonitor.Models;

public class Reminder
{
public int Id { get; set; }


public int ServerRoomId { get; set; }

public ServerRoom? ServerRoom { get; set; }

public DateTime CreatedAt { get; set; }

public string Message { get; set; } = "";

public bool IsRead { get; set; }


}
