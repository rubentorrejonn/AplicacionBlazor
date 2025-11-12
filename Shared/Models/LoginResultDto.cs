namespace UltimateProyect.Shared.Models;

public class LoginResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public int? UserId { get; set; }
}