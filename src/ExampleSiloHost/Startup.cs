namespace ExampleSiloHost;

/// <summary>
/// The startup class.
/// </summary>
public class Startup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </summary>
    public Startup()
    {
    }

    /// <summary>
    /// Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // Add configuration
        services.AddOptions();

        // Add the logger
        services.AddSingleton(Log.Logger);

        // Workaround to have a hosted background service available by DI
        services.AddSingleton<SiloHostService>();
        services.AddSingleton<IHostedService>(p => p.GetRequiredService<SiloHostService>());
    }

    /// <summary>
    /// Configures the application.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.Map("/dashboard", a => a.UseOrleansDashboard());

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapGet(
                    "/info",
                    async context =>
                    {
                        await context.Response.WriteAsync($"{DateTimeOffset.Now} SiloHost is up!");
                    });
            });
    }
}
