/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      TCP proxy service implementation.
 *      SINGLE CONNECTION ONLY.
 * 
 */


using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NetFlexor.Interfaces;

namespace NetFlexor.Service.Tcp
{
    public class TcpProxyService : NetFlexorBaseService
    {
        TcpServiceConfiguration _conf;
        ILogger<TcpProxyService> _logger;
        public TcpProxyService(IFlexorDataBuffer buffer, ILogger<TcpProxyService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as TcpServiceConfiguration;
            //return base.InitializeAsync(conf, stoppingToken);
            return Task.CompletedTask;
        }
        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("TCP proxy service is running.");
            _logger.LogInformation($"Server tcp connection keep alive: {_conf.GetKeepAliveInSeconds()} [ms]");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                // always skip the first run since there cannot be new data
                sw.Restart();
                try
                {
                    TcpProxyServer server = new(_conf.GetListeningIpAddress(), _conf.GetListeningPort(),
                        _conf.GetForwardIpAddress(), _conf.GetForwardPort(), _logger, _conf.AllowedSources,
                        _conf.GetKeepAliveInSeconds());
                    await server.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                sw.Stop();
                //// Try to keep the execution interval as close as possible
                //// If the execution took longer than the interval, skip the delay
                _logger.LogInformation($"Execution time: {sw.ElapsedMilliseconds} ms");
                if (interval > sw.ElapsedMilliseconds)
                    await Task.Delay((int)(interval - sw.ElapsedMilliseconds), stoppingToken);
            }
        }
    }
}
