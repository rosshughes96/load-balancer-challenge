namespace LoadBalancerProject.Api
{
    using System;
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Options;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;

    public static class Program
    {
        public static void Main(string[] args)
        {
            // Use WebApplication for Minimal APIs
            var builder = WebApplication.CreateBuilder(args);

            // Serilog console + rolling file
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/api-.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 5 * 1024 * 1024,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            builder.Logging.ClearProviders();
            builder.Host.UseSerilog();

            // DI registrations
            builder.Services.AddLoadBalancerCore(builder.Configuration);

            // Build the WebApplication (not IHost)
            var app = builder.Build();

            // Seed backends from config
            SeedBackendsFromConfig(app);

            // Map Minimal API endpoints
            app.MapLoadBalancerApi();

            app.Run();
        }

        private static void SeedBackendsFromConfig(WebApplication app)
        {
            var cfg = app.Services.GetRequiredService<IConfiguration>();
            var opts = cfg.GetSection("LoadBalancer").Get<LoadBalancerOptions>() ?? new LoadBalancerOptions();

            var registry = app.Services.GetRequiredService<IBackendRegistry>();
            foreach (var s in opts.Backends)
            {
                if (Uri.TryCreate(s, UriKind.Absolute, out var u))
                {
                    registry.Add(u);
                }
            }
        }
    }
}
