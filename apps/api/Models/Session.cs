namespace MemberOrgApi.Models;

public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    
    public User User { get; set; } = null!;
}