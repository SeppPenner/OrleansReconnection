namespace ExampleGrainInterfaces;

/// <summary>
/// The logger configuration.
/// </summary>
public static class LoggerConfig
{
    /// <summary>
    /// Gets the logger configuration.
    /// </summary>
    /// <returns>The logger configuration.</returns>
    public static LoggerConfiguration GetLoggerConfiguration()
    {
        // set up logging for data frame output
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext();
    }
}
