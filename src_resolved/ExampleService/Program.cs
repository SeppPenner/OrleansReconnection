namespace ExampleService;

/// <summary>
/// The program class.
/// </summary>
public class Program
{
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
    public static string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;

    /// <summary>
    /// The service name.
    /// </summary>
    public static AssemblyName ServiceName => Assembly.GetExecutingAssembly().GetName();

    /// <summary>
    /// Gets the urls.
    /// </summary>
    public static string Urls { get; private set; } = "http://0.0.0.0:5005";

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
            await CreateHostBuilder(args, currentLocation).Build().RunAsync().ConfigureAwait(false);
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
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
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
    /// Setup the logging.
    /// </summary>
    private static void SetupLogging()
    {
        const string CustomTemplate = "{Timestamp:dd.MM.yy HH:mm:ss.fff}\t[{Level:u3}]\t{Type}\t{Id}\t{Message}{NewLine}{Exception}";

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Orleans", LogEventLevel.Information)
            .Destructure.ByTransforming<IPEndPoint>(ep => new { Ip = ep.Address.ToString(), ep.Port })
            .Destructure.ByTransforming<IPAddress>(ip => ip.ToString())
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .WriteTo.Console(outputTemplate: CustomTemplate);

        loggerConfiguration.Enrich.WithProperty("InstanceKey", "Test");
        Log.Logger = loggerConfiguration.CreateLogger();
    }
}
