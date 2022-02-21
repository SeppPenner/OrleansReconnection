namespace ExampleSiloHost;

/// <summary>
/// The program class.
/// </summary>
public static class Program
{
    /// <summary>
    /// The invariant.
    /// </summary>
    private const string Invariant = "Npgsql";

    /// <summary>
    /// The database connection string.
    /// </summary>
    private const string DatabaseConnectionString = $"Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=example;Pooling=true;Timezone=Europe/Berlin;Enlist=false;Maximum Pool Size=400;ConvertInfinityDateTime=true";

    /// <summary>
    /// Checks whether the application is run in Docker environment or not. The variable is set in all Microsoft runtime images.
    /// </summary>

    public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?.ToLowerInvariant() == "true";

    /// <summary>
    /// Checks whether the application is run in Docker compose environment or not.
    /// </summary>

    public static bool InCompose => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_COMPOSE")?.ToLowerInvariant() == "true";

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    /// <summary>
    /// Gets or sets the urls the service is running on.
    /// </summary>
    public static string Urls { get; set; } = "http://0.0.0.0:8000";

    /// <summary>
    /// The service name.
    /// </summary>
    public static AssemblyName ServiceName => Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// The main method.
    /// </summary>
    /// <param name="args">Some arguments.</param>
    /// <returns>The result code.</returns>
    public static async Task<int> Main(string[] args)
    {
        SetupLogging();

        try
        {
            Log.Information("Starting {ServiceName}, Version {Version}...", ServiceName.Name, ServiceName.Version);
            Log.Information("Running on {Urls}...", Urls);
            var currentLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            await CreateHostBuilder(args, currentLocation).Build().RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return 0;
    }

    /// <summary>
    /// Creates the host builder.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <param name="currentLocation">The current assembly location.</param>
    /// <returns>A new <see cref="IHostBuilder"/>.</returns>
    private static IHostBuilder CreateHostBuilder(string[] args, string currentLocation) =>
        Host.CreateDefaultBuilder(args)
            .UseOrleans((ctx, builder) =>
            {
                builder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "service";
                });

                builder.UseAdoNetClustering(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = DatabaseConnectionString;
                });

                builder.UseAdoNetReminderService(options =>
                {
                    options.Invariant = Invariant;
                    options.ConnectionString = DatabaseConnectionString;
                });

                builder.AddSimpleMessageStreamProvider("SMSProvider");
                builder.AddMemoryGrainStorage("PubSubStore");

                var siloIpStr = "127.0.0.1";
                var siloIpAddress = IPAddress.Parse(siloIpStr);

                if (InCompose)
                {
                    var hostName = Dns.GetHostName();
                    siloIpAddress = Dns.GetHostAddresses(hostName).First();
                }

                builder.Configure<EndpointOptions>(
                    options =>
                    {
                        // The port to use for silo to silo communication
                        options.SiloPort = 11111;

                        // The port to use for the gateway
                        options.GatewayPort = 30000;

                        // The IP Address to advertise in the cluster
                        options.AdvertisedIPAddress = siloIpAddress;

                        // The socket used for silo to silo will bind to this endpoint
                        options.GatewayListeningEndpoint = new IPEndPoint(
                        siloIpAddress,
                        30000);

                        // The socket used by the gateway will bind to this endpoint
                        options.SiloListeningEndpoint = new IPEndPoint(
                        siloIpAddress,
                        11111);
                    });

                builder.ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(ExampleGrain).Assembly).WithReferences();
                });

                builder.ConfigureServices(services =>
                {
                    services.AddOptions();
                });

                builder.ConfigureLogging(logging =>
                {
                    logging.AddSerilog();
                });

                builder.UseDashboard(options =>
                {
                    options.HostSelf = false;
                });

                builder.UseLinuxEnvironmentStatistics();
            })
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseSerilog();
                    webBuilder.UseContentRoot(currentLocation);
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(Urls);
                })
            .UseSerilog()
            .UseWindowsService()
            .UseSystemd();

    /// <summary>
    /// Sets up the logging.
    /// </summary>
    private static void SetupLogging()
    {
        var customTemplate = "{Timestamp:dd.MM.yy HH:mm:ss.fff}\t[{Level:u3}]\t{Message}{NewLine}{Exception}";

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Orleans", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .Destructure.ByTransforming<IPEndPoint>(ep => new { Ip = ep.Address.ToString(), ep.Port })
            .Destructure.ByTransforming<IPAddress>(ip => ip.ToString())
            .WriteTo.Console(outputTemplate: customTemplate);

        Log.Logger = loggerConfiguration.CreateLogger();
    }
}
