namespace ExampleService;

/// <inheritdoc cref="BackgroundService"/>
/// <summary>
///     The main service class of the <see cref="ExampleService" />.
/// </summary>
public class ExampleService : BackgroundService
{
    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger logger = Log.Logger;

    /// <summary>
    /// A value indicating whether the Orleans client needs a reconnect or not.
    /// </summary>
    private bool reconnectOrleansClient = false;

    /// <summary>
    /// Gets the Orleans client.
    /// </summary>
    [NotNull]
    public IClusterClient? OrleansClient { get; internal set; }

    /// <summary>
    /// The service name.
    /// </summary>
    public static AssemblyName ServiceName => Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExampleService"/> class.
    /// </summary>
    public ExampleService()
    {
        this.logger = Log.ForContext("Type", nameof(ExampleService));
    }

    /// <inheritdoc cref="BackgroundService"/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var gateways = new IPEndPoint[]
        {
            new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000)
        };

        if (Program.InCompose)
        {
            var addresses = await Dns.GetHostAddressesAsync("example.silohost", cancellationToken).ConfigureAwait(false);
            gateways = addresses.Select(a => new IPEndPoint(a, 30000)).ToArray();
        }

        this.OrleansClient = new ClientBuilder()
            .Configure<ClusterOptions>(
                options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "service";
                })
            .UseStaticClustering(gateways)
            .ConfigureApplicationParts(
                parts =>
                {
                    parts.AddApplicationPart(typeof(IExampleGrain).Assembly).WithReferences();
                })
            .ConfigureLogging(logging => logging.AddSerilog())
            .AddSimpleMessageStreamProvider("SMSProvider")
            .Build();

        this.logger.Information("Connecting to Orleans Silo");

        var isConnected = await this.ConnectOrleansClient(cancellationToken).ConfigureAwait(false);

        if (!isConnected)
        {
            this.logger.Fatal("Silo is down, terminating");
            throw new Exception("Silo connect failed");
        }

        this.logger.Information("Orleans connected");
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="BackgroundService"/>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Cyclic check whether the client is initialized or not.
            try
            {
                if (!this.OrleansClient.IsInitialized)
                {
                    await this.OrleansClient.Close().ConfigureAwait(false);
                    this.reconnectOrleansClient = true;
                }
            }
            catch (Exception ex)
            {
                this.reconnectOrleansClient = true;
                this.logger.Error("Closing uninitialized Orleans client failed", ex);
            }

            if (!this.reconnectOrleansClient)
            {
                // Cyclic check whether the grain calls are ok or not (GrainTypeResolver error detection).
                try
                {
                    var exampleGrain =
                        this.OrleansClient.GetGrain<IExampleGrain>("Grain1");
                    _ = await exampleGrain.GetAllData().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.reconnectOrleansClient = true;
                    this.logger.Error("The connection to the Silo failed", ex);
                }
            }

            // Reconnect if an error was detected.
            if (this.reconnectOrleansClient)
            {
                var isConnected = await this.ConnectOrleansClient(cancellationToken).ConfigureAwait(false);

                if (isConnected)
                {
                    this.logger.Error("Reconnect to silo successful");
                }
                else
                {
                    this.logger.Error("Reconnect to silo failed");
                }
            }

            this.LogMemoryInformation();
            await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Connects the Orleans client.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<bool> ConnectOrleansClient(CancellationToken cancellationToken)
    {
        var connectTask = this.OrleansClient.Connect();

        // Unfortunately, this timeout check is required.
        var delay = Task.Delay(6000, cancellationToken);
        var result = await Task.WhenAny(connectTask, delay).ConfigureAwait(false);

        return result != delay && !result.IsFaulted;
    }

    /// <summary>
    /// Logs the memory information.
    /// </summary>
    private void LogMemoryInformation()
    {
        var totalMemory = GC.GetTotalMemory(false);
        var memoryInfo = GC.GetGCMemoryInfo();
        var divider = 1048576.0;
        this.logger.Information(
            "Heartbeat for service {ServiceName}: Total {Total}, heap size: {HeapSize}, memory load: {MemoryLoad}.",
            ServiceName.Name, $"{totalMemory / divider:N3}", $"{memoryInfo.HeapSizeBytes / divider:N3}", $"{memoryInfo.MemoryLoadBytes / divider:N3}");
    }
}
