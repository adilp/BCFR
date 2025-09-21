namespace MemberOrgApi.Services;

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string Base64Content { get; set; } = string.Empty;
    public string? ContentType { get; set; }
}

