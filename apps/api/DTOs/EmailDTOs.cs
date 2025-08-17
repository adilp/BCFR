namespace MemberOrgApi.DTOs;

public class SendEmailRequest
{
    public List<string> ToEmails { get; set; } = new List<string>();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
}

public class SendEmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
}