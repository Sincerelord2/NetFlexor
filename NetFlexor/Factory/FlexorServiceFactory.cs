/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This is the service factory for the NetFlexor application.
 * 
 */

using Microsoft.Extensions.DependencyInjection;
using NetFlexor.Interfaces;

namespace NetFlexor.Factory
{
    public class FlexorServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _serviceDescriptors;
        private readonly Dictionary<string, Type> _services = new Dictionary<string, Type>();
        public FlexorServiceFactory(IServiceProvider serviceProvider, IServiceCollection serviceDescriptors)
        {
            _serviceProvider = serviceProvider;
            _serviceDescriptors = serviceDescriptors;
            BuildServices();
        }

        /// <summary>
        /// Build the services
        /// </summary>
        private void BuildServices()
        {
            foreach (var service in _serviceDescriptors)
            {
                if (service.ImplementationType is not null && 
                    service.ImplementationType.IsAssignableTo(typeof(INetFlexorService)))
                {
                    _services.Add(service.ImplementationType.Name, service.ImplementationType);
                }
            }
        }

        /// <summary>
        /// Try to get the service by name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool TryGetSyncService(string serviceName, out INetFlexorService? service)
        {
            if (_services.TryGetValue(serviceName, out var syncService))
            {
                service = _serviceProvider.GetRequiredService(syncService) as INetFlexorService;
                return true;
            }
            service = null;
            return false;
        }
    }
}
