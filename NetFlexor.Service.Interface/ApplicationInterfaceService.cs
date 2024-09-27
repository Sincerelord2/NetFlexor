/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Interface service for the application for the outside applications to connect to the service.
 * 
 */

// TODO: Change this service to be a interface to outside applications
//       Create the interface with gRPC and implement it here
//       This way data and configuration can be shared with other outside applications
//       Make the implementation the most secure way possible.
//       Maybe use a token system to authenticate the outside applications
//       Add also a source check for the allowed MAC addresses to connect to the service
//       So the security is not only based only on the token
//       Also add a way to revoke the token if it is compromised
//       Maybe add a way to revoke the token if the source MAC address is compromised
//       Add a way to revoke the token if the source MAC address is not in the allowed list and it uses the token that does not belong to it
//       Other security measures can be added later or during the development

//      Steps to implement gRPC:
//      1. Rename this service to NetFlexorApplicationInterfaceService
//      2. Add support to the gRPC
//      3. Implement the interface and the gRPC structure in this service and document how to use it
//      4. Add the security measures -> Token and mac based connection
//      5. 

using System.Diagnostics;
using NetFlexor.Interfaces;
using Microsoft.Extensions.Logging;

namespace NetFlexor.Service.Interface
{
    public class ApplicationInterfaceService : NetFlexorBaseService
    {
        ApplicationInterfaceConfiguration _conf;
        ILogger<ApplicationInterfaceService> _logger;
        public ApplicationInterfaceService(IFlexorDataBuffer buffer, ILogger<ApplicationInterfaceService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as ApplicationInterfaceConfiguration;
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("Running gRPC interface service");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                try
                {
                    await RunServiceOpertaions(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                sw.Stop();

                //// Try to keep the execution interval as close as possible
                //// If the execution took longer than the interval, skip the delay
                _logger.LogDebug($"Execution time: {sw.ElapsedMilliseconds} ms");
                if (interval > sw.ElapsedMilliseconds)
                    await Task.Delay((int)(interval - sw.ElapsedMilliseconds), stoppingToken);
            }
        }

        private async Task RunServiceOpertaions(CancellationToken cancellationToken)
        {

        }
    }
}
