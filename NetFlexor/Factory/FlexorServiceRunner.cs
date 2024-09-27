/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This is the service configuration builder and runner for the NetFlexor application.
 *      It will read the configuration file, generate service configurations, generate services and run them.
 * 
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using NetFlexor.Interfaces;

namespace NetFlexor.Factory
{
    public class FlexorServiceRunner : BackgroundService
    {
        private ILogger<FlexorServiceRunner> _logger;
        private readonly FlexorServiceFactory _syncFactory;
        private NetFlexorConfiguration _config;
        private List<INetFlexorService> _services = new();

        public FlexorServiceRunner(IOptions<ApplicationOptions> appConfig, FlexorServiceFactory syncFactory)
        {
            _syncFactory = syncFactory;
            var deserializer = new DeserializerBuilder().Build();
            // Read the yaml configuration file
            _config = deserializer.Deserialize<NetFlexorConfiguration>(File.ReadAllText(appConfig.Value.ConfigurationFile));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Link services to their configuration
            var serviceList = new List<(INetFlexorService, IFlexorServiceConfiguration)>();
            foreach(var conf in _config.getServicesConfiguration)
            {
                // TODO: Add check for duplicate service names, because
                // the service name is used as a key in the dictionary

                var name = conf.GetTypeServiceName();
                if (name is null || !_syncFactory.TryGetSyncService(name, out var service))
                    continue;

                // Populate the buffer configuration if it is missing
                // Boolean values are not populated as they are not nullable
                conf.PopulateNetFlexorConfiguration(_config);

                serviceList.Add((service!, conf));
            }

            // Start the services
            var tasks = new List<Task>();
            foreach (var service in serviceList)
            {
                // Get the services to a new list to handle later
                _services.Add(service.Item1);

                // Start the services
                tasks.Add(StartNetFlexorServiceAsync(service.Item1, service.Item2, stoppingToken));
            }
            await Task.WhenAll(tasks);

            // TODO: Add error handling and crash recovery for the services?
        }
        private async Task StartNetFlexorServiceAsync(INetFlexorService service, IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            await service.InitializeAsync(conf, stoppingToken);
            await service.WorkAsync(stoppingToken);
        }
    }
}