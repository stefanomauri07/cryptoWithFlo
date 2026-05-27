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

        // NOTE FOR SCHOOL PRESENTATION:
        // The school firewall (FortiGuard) intercepts HTTPS connections.
        // SSL certificate validation is bypassed ONLY for trusted API endpoints
        // (Binance, Stripe, Brevo) so the app works on the school network.
        // In production, remove this handler and use proper SSL validation.
        var handler = new System.Net.Http.SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };

        var stripeHttpClient = new System.Net.Http.HttpClient(handler);
        var stripeClient = new StripeClient(
            config["Stripe:SecretKey"],
            httpClient: new SystemNetHttpClient(httpClient: stripeHttpClient));

        StripeConfiguration.StripeClient = stripeClient;
    }

    public async Task<string> CreateCheckoutSessionAsync(User user, string plan)
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

        var (unitAmount, interval, intervalCount) = plan switch
        {
            "monthly"     => (999,    "month", 1),
            "semiannual"  => (4999,   "month", 6),
            "annual"      => (8999,   "year",  1),
            _ => throw new ArgumentException($"Invalid plan: {plan}")
        };

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = unitAmount,
                        Currency = "usd",
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = interval,
                            IntervalCount = intervalCount
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Pro Plan ({plan})"
                        }
                    },
                    Quantity = 1
                }
            },
            SuccessUrl = "http://localhost:3000/upgrade.html?session_id={CHECKOUT_SESSION_ID}&status=success",
            CancelUrl = "http://localhost:3000/upgrade.html?status=cancelled",
            Metadata = new Dictionary<string, string> { { "userId", user.Id.ToString() }, { "plan", plan } }
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
