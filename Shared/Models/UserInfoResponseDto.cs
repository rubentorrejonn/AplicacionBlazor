namespace UltimateProyect.Shared.Models;

public class UserInfoResponseDto
{
    public bool IsAuthenticated { get; set; }
    public string? UserName { get; set; }
    public int? UserId { get; set; }
    public string? Roles { get; set; }
}