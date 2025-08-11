using Stripe;
using Stripe.Checkout;
using MemberOrgApi.Models;
using MemberOrgApi.Data;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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
        private readonly IServiceScopeFactory _scopeFactory;

        public StripeService(IOptions<StripeSettings> stripeSettings, ILogger<StripeService> logger, IServiceScopeFactory scopeFactory)
        {
            _stripeSettings = stripeSettings.Value;
            _logger = logger;
            _scopeFactory = scopeFactory;
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
            
            if (!session.Metadata.TryGetValue("userId", out var userIdStr) || 
                !int.TryParse(userIdStr, out var userId))
            {
                _logger.LogError($"Invalid or missing userId in session metadata");
                return;
            }

            var membershipTier = session.Metadata["membershipTier"];
            var subscriptionId = session.SubscriptionId;
            var customerId = session.CustomerId;

            // Get subscription details from Stripe
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId);

            // Create a new scope for database operations
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if subscription already exists
            var existingSubscription = await dbContext.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

            if (existingSubscription != null)
            {
                _logger.LogInformation($"Subscription {subscriptionId} already exists in database");
                return;
            }

            // Create new subscription record
            // In Stripe.NET v48, CurrentPeriodEnd is on SubscriptionItem, not Subscription
            var subscriptionItem = subscription.Items.Data.FirstOrDefault();
            var currentPeriodEnd = subscriptionItem?.CurrentPeriodEnd ?? subscription.Created.AddYears(1);
            
            var membershipSubscription = new MembershipSubscription
            {
                UserId = userId,
                MembershipTier = membershipTier,
                StripeCustomerId = customerId,
                StripeSubscriptionId = subscriptionId,
                Status = subscription.Status,
                StartDate = subscription.Created,
                EndDate = currentPeriodEnd,
                NextBillingDate = currentPeriodEnd,
                Amount = (subscription.Items.Data[0].Price.UnitAmountDecimal ?? 0) / 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.MembershipSubscriptions.Add(membershipSubscription);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation($"Subscription {subscriptionId} saved to database for user {userId}");
        }

        private async Task HandleSubscriptionDeleted(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            
            _logger.LogInformation($"Subscription cancelled: {subscription.Id}");
            
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var membershipSubscription = await dbContext.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);

            if (membershipSubscription != null)
            {
                membershipSubscription.Status = "cancelled";
                membershipSubscription.EndDate = DateTime.UtcNow;
                membershipSubscription.UpdatedAt = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Subscription {subscription.Id} marked as cancelled in database");
            }
        }

        private async Task HandleSubscriptionUpdated(Stripe.Subscription? subscription)
        {
            if (subscription == null) return;
            
            _logger.LogInformation($"Subscription updated: {subscription.Id}");
            
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var membershipSubscription = await dbContext.MembershipSubscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);

            if (membershipSubscription != null)
            {
                var subscriptionItem = subscription.Items.Data.FirstOrDefault();
                membershipSubscription.Status = subscription.Status;
                membershipSubscription.NextBillingDate = subscriptionItem?.CurrentPeriodEnd ?? membershipSubscription.NextBillingDate;
                membershipSubscription.UpdatedAt = DateTime.UtcNow;
                
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Subscription {subscription.Id} updated in database with status: {subscription.Status}");
            }
        }
    }
}