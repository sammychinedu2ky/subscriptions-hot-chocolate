using HotChocolate.Subscriptions;
using HotChocolate.Execution;

var builder = WebApplication.CreateBuilder(args);

//Add GraphQL services
builder.Services.AddGraphQLServer()
.AddSubscriptionType<Subscription>()
.AddInMemorySubscriptions().ConfigureSchema(s =>s.ModifyOptions(o => o.StrictValidation = false));

// Add background host
builder.Services.AddHostedService<CurrencyUpdateService>();
var app = builder.Build();
app.MapGet("/",()=>"I'm from concise");
app.UseWebSockets();
app.MapGraphQL();
app.Run();

public class Currency
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    public double RateToADollar { get; set; }
}

public class Subscription
{
    [SubscribeAndResolve]
    public async ValueTask<ISourceStream<Currency>> OnCurrencyUpdate(string currencyCode,
       [Service] ITopicEventReceiver receiver)
    {
        string topic = $"{currencyCode}-Updates";
        return await receiver.SubscribeAsync<string, Currency>(topic);
    }
}


public class CurrencyUpdateService : BackgroundService
{
    private readonly ITopicEventSender _eventSender;
    private readonly PeriodicTimer _timer;

    public CurrencyUpdateService(ITopicEventSender sender)
    {
        _eventSender = sender;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _timer.WaitForNextTickAsync(stoppingToken);
            double randomRate = Random.Shared.NextDouble();
            var nairaUpdate = new Currency
            {
                Code = "NGN",
                Name = "Naira",
                RateToADollar = randomRate * (30 - 26) + 26

            };
            var cedisUpdate = new Currency
            {
                Code = "GHS",
                Name = "Cedis",
                RateToADollar = randomRate * (6 - 4) + 4
            };
            await _eventSender.SendAsync($"NGN-Updates", nairaUpdate, stoppingToken);
            await _eventSender.SendAsync($"GHS-Updates", cedisUpdate, stoppingToken);
        }
    }
}

