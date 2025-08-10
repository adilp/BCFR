using Stripe;
using Stripe.Checkout;
using MemberOrgApi.Models;
using Microsoft.Extensions.Options;

namespace MemberOrgApi.Services
{
    public interface IStripeService
    {
        Task<Stripe.Checkout.Session> CreateCheckoutSessionAsync(string email, string membershipTier, string userId);
        Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId);
        Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId);
        Task HandleWebhookAsync(string json, string stripeSignature);
    }

    public class StripeService : IStripeService
    {
        private readonly StripeSettings _stripeSettings;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IOptions<StripeSettings> stripeSettings, ILogger<StripeService> logger)
        {
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }

        public async Task<Stripe.Checkout.Session> CreateCheckoutSessionAsync(string email, string membershipTier, string userId)
        {
            var priceId = membershipTier.ToLower() switch
            {
                "over40" => _stripeSettings.PriceIds.Over40,
                "under40" => _stripeSettings.PriceIds.Under40,
                "student" => _stripeSettings.PriceIds.Student,
                _ => throw new ArgumentException($"Invalid membership tier: {membershipTier}")
            };

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    }
                },
                Mode = "subscription",
                SuccessUrl = "https://birminghamforeignrelations.org/membership/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://birminghamforeignrelations.org/membership",
                CustomerEmail = email,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "membershipTier", membershipTier }
                }
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }

        public async Task<Stripe.Subscription> GetSubscriptionAsync(string subscriptionId)
        {
            var service = new SubscriptionService();
            return await service.GetAsync(subscriptionId);
        }

        public async Task<Stripe.Subscription> CancelSubscriptionAsync(string subscriptionId)
        {
            var service = new SubscriptionService();
            var options = new SubscriptionCancelOptions();
            return await service.CancelAsync(subscriptionId, options);
        }

        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _stripeSettings.WebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                        await HandleCheckoutSessionCompleted(session);
                        break;
                    case "customer.subscription.deleted":
                        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                        await HandleSubscriptionDeleted(subscription);
                        break;
                    case "customer.subscription.updated":
                        var updatedSubscription = stripeEvent.Data.Object as Stripe.Subscription;
                        await HandleSubscriptionUpdated(updatedSubscription);
                        break;
                    default:
                        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                        break;
                }
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe webhook error");
                throw;
            }
        }

        private async Task HandleCheckoutSessionCompleted(Stripe.Checkout.Session? session)
        {
            if (session == null) return;
            
            _logger.LogInformation($"Payment successful for session {session.Id}");
            
            var userId = session.Metadata["userId"];
            var membershipTier = session.Metadata["membershipTier"];
            var subscriptionId = session.SubscriptionId;
            
            // TODO: Update user's subscription status in database
            // You'll need to inject your database context here and update the user's subscription
            
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionDeleted(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            
            _logger.LogInformation($"Subscription cancelled: {subscription.Id}");
            
            // TODO: Update user's subscription status in database
            
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionUpdated(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            
            _logger.LogInformation($"Subscription updated: {subscription.Id}");
            
            // TODO: Handle subscription updates (e.g., renewal, payment failure)
            
            await Task.CompletedTask;
        }
    }
}