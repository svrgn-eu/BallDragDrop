using Microsoft.Extensions.DependencyInjection;
using System;
using BallDragDrop.ViewModels;
using BallDragDrop.Services;
using BallDragDrop.Contracts;

namespace BallDragDrop.Bootstrapper
{
    /// <summary>
    /// Central static bootstrapper class for dependency injection configuration
    /// </summary>
    public static class ServiceBootstrapper
    {
        #region Fields

        /// <summary>
        /// The configured service provider instance
        /// </summary>
        private static ServiceProvider? _serviceProvider;

        /// <summary>
        /// Lock object for thread-safe initialization
        /// </summary>
        private static readonly object _lock = new object();

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the configured service provider
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the service provider has not been initialized</exception>
        public static ServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceBootstrapper has not been initialized. Call Initialize() first.");
                }
                return _serviceProvider;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes the service container with all application services
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_serviceProvider != null)
                {
                    return; // Already initialized
                }

                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Configures all application services in the dependency injection container
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        public static void ConfigureServices(IServiceCollection services)
        {
            // Register core application services
            RegisterCoreServices(services);
            
            // Register logging services (will be implemented in subtask 7.2)
            RegisterLoggingServices(services);
            
            // Register ViewModels
            RegisterViewModels(services);
            
            // Register other application services
            RegisterApplicationServices(services);
        }

        /// <summary>
        /// Registers core application services
        /// </summary>
        private static void RegisterCoreServices(IServiceCollection services)
        {
            // Core services will be registered here
            services.AddSingleton<SettingsManager>(provider => 
                new SettingsManager(provider.GetRequiredService<ILogService>()));
            
            // Register ConfigurationService
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // ImageService uses static methods, so no registration needed
            // EventThrottler and PerformanceMonitor are created as needed with specific parameters
        }

        /// <summary>
        /// Registers logging-related services
        /// </summary>
        private static void RegisterLoggingServices(IServiceCollection services)
        {
            // Initialize Log4NET configuration
            InitializeLog4NetConfiguration();
            
            // Register ILogService as singleton with Log4NetService implementation
            services.AddSingleton<ILogService, Log4NetService>();
            
            // Register IExceptionHandlingService
            services.AddSingleton<IExceptionHandlingService, ExceptionHandlingService>();
            
            // Register method interception services
            services.AddTransient<IMethodLoggingInterceptor, MethodLoggingInterceptor>();
        }

        /// <summary>
        /// Initializes Log4NET configuration
        /// </summary>
        private static void InitializeLog4NetConfiguration()
        {
            // Basic Log4NET configuration - will be enhanced in task 3
            log4net.Config.BasicConfigurator.Configure();
        }

        /// <summary>
        /// Registers ViewModels for dependency injection
        /// </summary>
        private static void RegisterViewModels(IServiceCollection services)
        {
            services.AddTransient<BallViewModel>(provider => 
                new BallViewModel(
                    provider.GetRequiredService<ILogService>(),
                    null, // ImageService will be created internally
                    provider.GetRequiredService<IConfigurationService>()));
        }

        /// <summary>
        /// Registers other application-specific services
        /// </summary>
        private static void RegisterApplicationServices(IServiceCollection services)
        {
            // Additional application services can be registered here
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance</returns>
        public static T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a service of the specified type, or null if not found
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve</typeparam>
        /// <returns>The service instance or null</returns>
        public static T? GetOptionalService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Disposes the service provider and cleans up resources
        /// </summary>
        public static void Dispose()
        {
            lock (_lock)
            {
                _serviceProvider?.Dispose();
                _serviceProvider = null;
            }
        }

        #endregion Methods
    }
}