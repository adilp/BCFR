using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MemberOrgApi.Services;
using System.Security.Claims;
using Stripe;

namespace MemberOrgApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly ILogger<StripeController> _logger;
        private readonly IConfiguration _configuration;

        public StripeController(IStripeService stripeService, ILogger<StripeController> logger, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _logger = logger;
            _configuration = configuration;
        }

        // TODO: Remove this endpoint before production - for testing only
        [HttpGet("config-check")]
        public IActionResult ConfigCheck()
        {
            var hasSecretKey = !string.IsNullOrEmpty(_configuration["Stripe:SecretKey"]);
            var hasPublishableKey = !string.IsNullOrEmpty(_configuration["Stripe:PublishableKey"]);
            var hasWebhookSecret = !string.IsNullOrEmpty(_configuration["Stripe:WebhookSecret"]);
            var hasPriceIds = !string.IsNullOrEmpty(_configuration["Stripe:PriceIds:Over40"]);
            
            return Ok(new
            {
                configLoaded = new
                {
                    hasSecretKey,
                    hasPublishableKey,
                    hasWebhookSecret,
                    hasPriceIds
                },
                publishableKey = _configuration["Stripe:PublishableKey"], // Safe to expose
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                {
                    return BadRequest("User information not found");
                }

                var session = await _stripeService.CreateCheckoutSessionAsync(
                    email,
                    request.MembershipTier,
                    userId
                );

                return Ok(new { sessionId = session.Id, url = session.Url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, "An error occurred while creating the checkout session");
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeSignature = Request.Headers["Stripe-Signature"];
                await _stripeService.HandleWebhookAsync(json, stripeSignature);
                return Ok();
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "Stripe webhook error");
                return BadRequest();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Webhook processing error");
                return StatusCode(500);
            }
        }

        [HttpGet("subscription/{subscriptionId}")]
        [Authorize]
        public async Task<IActionResult> GetSubscription(string subscriptionId)
        {
            try
            {
                var subscription = await _stripeService.GetSubscriptionAsync(subscriptionId);
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription");
                return StatusCode(500, "An error occurred while retrieving the subscription");
            }
        }

        [HttpPost("cancel-subscription/{subscriptionId}")]
        [Authorize]
        public async Task<IActionResult> CancelSubscription(string subscriptionId)
        {
            try
            {
                var subscription = await _stripeService.CancelSubscriptionAsync(subscriptionId);
                return Ok(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription");
                return StatusCode(500, "An error occurred while cancelling the subscription");
            }
        }
    }

    public class CreateCheckoutSessionRequest
    {
        public string MembershipTier { get; set; } = string.Empty;
    }
}