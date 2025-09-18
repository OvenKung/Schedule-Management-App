namespace Schedule_Management.Models;

public class AttendanceRecord
{
    public string Email { get; set; } = "";
    public string Status { get; set; } = "";   // on_time | late | absent | pending | leave
    public string TimeRange { get; set; } = "";
    public string ImageUrl { get; set; } = "";   // URL to the image
    public string Reason { get; set; } = "";   // reason for leave (blank for on-time/late)


}