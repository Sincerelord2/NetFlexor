/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Random data generator service for debugging purposes.
 * 
 */

using System.Diagnostics;
using NetFlexor.Interfaces;
using Microsoft.Extensions.Logging;

namespace NetFlexor.Service.RandomServiceObject
{
    public class RandomService : NetFlexorBaseService
    {
        RandomServiceConfiguration _conf;
        ILogger<RandomService> _logger;
        public RandomService(IFlexorDataBuffer buffer, ILogger<RandomService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as RandomServiceConfiguration;
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("Random service is running.");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                try
                {
                    List<object> ints = new();
                    List<string> tags = new();
                    //List<DateTime> dateTimes = new();
                    for (int i = 0; i < _conf.Count; i++)
                    {
                        ints.Add(new Random().Next(_conf.Min, _conf.Max));
                        tags.Add("random_" + i);
                        //dateTimes.Add(DateTime.Now);
                    }
                    var container = new FlexorDataBufferContainer(_conf.Name);/*
                    //var container = new SyncDataBufferContainer(_conf.Count, _conf.Name);
                        , ints.ToArray(), dateTimes.ToArray(), tags.ToArray());*/
                    //container.AddArrayDataToContainer(ints.ToArray(), tags.ToArray(), dateTimes.ToArray());
                    container.AddArrayDataToContainer(tags, ints, DateTime.UtcNow);
                    //container.timeStamp = DateTime.Now;
                    EnqueueDataToBuffer(container);
                    //var buffer = GetBuffer();
                    _logger.LogDebug("Data created and added to buffer.");
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                sw.Stop();
                // Try to keep the execution interval as close as possible
                // If the execution took longer than the interval, skip the delay
                _logger.LogDebug($"Execution time: {sw.ElapsedMilliseconds} ms");
                if (interval > sw.ElapsedMilliseconds)
                    await Task.Delay((int)(interval - sw.ElapsedMilliseconds), stoppingToken);
            }
        }
    }
}
