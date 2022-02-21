namespace ExampleService;

/// <summary>
/// The startup class.
/// </summary>
public class Startup
{
    /// <summary>
    /// Configures the services.
    /// </summary>
    /// <param name="services">The services.</param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        services.AddMvc().AddRazorPagesOptions(options => { options.RootDirectory = "/"; })
            .AddDataAnnotationsLocalization();

        // Workaround to have a hosted background service available by DI
        services.AddSingleton<ExampleService>();
        services.AddSingleton<IHostedService>(p => p.GetRequiredService<ExampleService>());
    }

    /// <summary>
    /// This method gets called by the runtime.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="env">The web hosting environment.</param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();
        app.UseRouting();

        _ = app.ApplicationServices.GetService<ExampleService>();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet(
                "/",
                async context =>
                {
                    var sb = new StringBuilder();
                    sb.Append("Hello there!");
                    await context.Response.WriteAsync($"{sb}").ConfigureAwait(false);
                });
        });
    }
}
