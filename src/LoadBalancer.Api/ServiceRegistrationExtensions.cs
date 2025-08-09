namespace LoadBalancerProject.Api
{
    using System;
    using LoadBalancerProject.Backends;
    using LoadBalancerProject.Configuration;
    using LoadBalancerProject.Health;
    using LoadBalancerProject.LoadBalancing;
    using LoadBalancerProject.LoadBalancing.Strategies;
    using LoadBalancerProject.Metrics;
    using LoadBalancerProject.Options;
    using LoadBalancerProject.Proxy;
    using LoadBalancerProject.Queue;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// DI registration helpers for the LoadBalancer host.
    /// </summary>
    public static class ServiceRegistrationExtensions
    {
        /// <summary>
        /// Registers options, validators, strategies, metrics, and hosted services for the LoadBalancer.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddLoadBalancerCore(this IServiceCollection services, IConfiguration configuration)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            // Bind options and validate at startup (fail-fast)
            services
                .AddOptions<LoadBalancerOptions>()
                .Bind(configuration.GetSection("LoadBalancer"))
                .ValidateOnStart();

            services
                .AddOptions<TcpForwarderOptions>()
                .Bind(configuration.GetSection("TcpForwarder"))
                .ValidateOnStart();

            // Validators
            services.AddSingleton<IValidateOptions<LoadBalancerOptions>, LoadBalancerOptionsValidator>();
            services.AddSingleton<IValidateOptions<TcpForwarderOptions>, TcpForwarderOptionsValidator>();

            // Dynamic config (strategy + health interval)
            var lbOpts = configuration.GetSection("LoadBalancer").Get<LoadBalancerOptions>() ?? new LoadBalancerOptions();
            services.AddSingleton<IDynamicConfig>(_ => new InMemoryDynamicConfig(lbOpts.Strategy, lbOpts.HealthCheckIntervalSeconds));

            // TCP forwarder options as a singleton instance used by the forwarder
            var tcpOpts = configuration.GetSection("TcpForwarder").Get<TcpForwarderOptions>() ?? new TcpForwarderOptions();
            services.AddSingleton(tcpOpts);

            // Registries, trackers, metrics
            services.AddSingleton<IBackendRegistry, BackendRegistry>();
            services.AddSingleton<IBackendQueueTracker, BackendQueueTracker>();
            services.AddSingleton<IConnectionMetrics, ConnectionMetrics>();

            // Strategies & provider
            services.AddSingleton<RoundRobinStrategy>();
            services.AddSingleton<LeastQueueStrategy>();
            services.AddSingleton<IStrategyProvider, StrategyProvider>();

            // Health checker (background) and facade
            services.AddSingleton<DynamicHealthChecker>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DynamicHealthChecker>());
            services.AddSingleton<IHealthChecker>(sp => sp.GetRequiredService<DynamicHealthChecker>());

            // Core LB + forwarder + TCP listener (background)
            services.AddSingleton<ILoadBalancer, LoadBalancerProject.LoadBalancing.LoadBalancer>();
            services.AddSingleton<IRequestForwarder, TcpRequestForwarder>();
            services.AddHostedService<TcpLoadBalancerService>();

            return services;
        }
    }
}
