using CryptoApp.Models;
using Stripe;
using Stripe.Checkout;

namespace CryptoApp.Services;

public class StripeService
{
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration config, ILogger<StripeService> logger)
    {
        _config = config;
        _logger = logger;
        StripeConfiguration.ApiKey = config["STRIPE_SECRET_KEY"];

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        StripeConfiguration.HttpClient = new HttpClient(handler);
    }

    public async Task<string> CreateCheckoutSessionAsync(User user)
    {
        var customerId = user.StripeCustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Metadata = new Dictionary<string, string> { { "userId", user.Id.ToString() } }
            });
            customerId = customer.Id;
            user.StripeCustomerId = customerId;
        }

        var priceId = _config["STRIPE_PRO_PRICE_ID"]!;
        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions { Price = priceId, Quantity = 1 }
            },
            SuccessUrl = "http://localhost:3000/upgrade.html?session_id={CHECKOUT_SESSION_ID}&status=success",
            CancelUrl = "http://localhost:3000/upgrade.html?status=cancelled",
            Metadata = new Dictionary<string, string> { { "userId", user.Id.ToString() } }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task CancelSubscriptionAsync(User user)
    {
        if (string.IsNullOrEmpty(user.StripeCustomerId)) return;

        var subService = new SubscriptionService();
        var subs = await subService.ListAsync(new SubscriptionListOptions
        {
            Customer = user.StripeCustomerId,
            Status = "active",
            Limit = 1
        });

        var activeSub = subs.FirstOrDefault();
        if (activeSub != null)
        {
            await subService.UpdateAsync(activeSub.Id, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });
        }
    }
}
