namespace ExampleSiloHost;

/// <inheritdoc />
public class SiloHostService : BackgroundService
{
    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<SiloHostService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SiloHostService"/> class.
    /// </summary>
    /// <param name="configuration">The silo host configuration.</param>
    /// <param name="pgUtil">The database connection helper.</param>
    /// <param name="logger">The logger.</param>
    public SiloHostService(ILogger<SiloHostService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Starting silo host...");
        await base.StartAsync(cancellationToken);
        this.logger.LogInformation("Silo host has started.");
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Stopping silo host...");
        await base.StopAsync(cancellationToken);
        this.logger.LogInformation("Silo host is stopped.");
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(10000, cancellationToken);
            this.logger.LogInformation("Heartbeat.");
        }
    }
}
