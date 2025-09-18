namespace Schedule_Management.Models;

public class UserInfo
{
    public string Email { get; set; } = "";
    public string Fullname { get; set; } = "";
    public string Role { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Status { get; set; } = "";   // active | inactive
}