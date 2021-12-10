namespace ExampleGrains;

/// <inheritdoc cref="IExampleGrain"/>
public class ExampleGrain : Grain, IExampleGrain
{
    /// <summary>
    /// The logger.
    /// </summary>
    private ILogger? logger;

    /// <inheritdoc cref="Grain"/>
    public override async Task OnActivateAsync()
    {
        await base.OnActivateAsync();

        var grainId = this.GetPrimaryKeyString();

        this.logger = LoggerConfig.GetLoggerConfiguration().Enrich.WithProperty("GrainId", grainId)
            .WriteTo
            .Sink((ILogEventSink)Log.Logger).CreateLogger();

        this.logger.Information("Grain for id {GrainId} is activating...", grainId);
    }

    /// <inheritdoc cref="IExampleGrain"/>
    public Task<List<DtoData>> GetAllData()
    {
        var data = new List<DtoData>
        {
            new DtoData { Id = "1" },
            new DtoData { Id = "2" }
        };

        return Task.FromResult(data);
    }
}
