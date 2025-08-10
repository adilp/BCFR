namespace MemberOrgApi.Models
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public StripePriceIds PriceIds { get; set; } = new();
    }

    public class StripePriceIds
    {
        public string Over40 { get; set; } = string.Empty;
        public string Under40 { get; set; } = string.Empty;
        public string Student { get; set; } = string.Empty;
    }
}