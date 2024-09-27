/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This service will monitor the data in the buffer and move the data to disk if the buffer is full.
 *      Alternatively, the service can remove the oldest data from the memory or disk buffer.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NetFlexor.Interfaces;
using NetFlexor.ServiceHelpers;

namespace NetFlexor.Service.BufferHandler
{
    /// <summary>
    /// Service to handle buffer operations. <br></br>
    /// Like adding, removing, and checking buffer size.
    /// </summary>
    public class BufferHandlerService : NetFlexorBaseService
    {
        ILogger<BufferHandlerService> _logger;
        BufferHandlerServiceConfiguration _conf;
        List<DirectoryHandler> _dirHandlers;

        public BufferHandlerService(IFlexorDataBuffer buffer, ILogger<BufferHandlerService> logger) : base(buffer)
        {
            _logger = logger;
        }

        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as BufferHandlerServiceConfiguration;

            // This will create the handlers for the service buffer directories
            _dirHandlers = new List<DirectoryHandler>();
            foreach (var item in _conf.serviceConfigurations)
            {
                var itemBuffer = item.GetFlexorServiceBufferConfiguration();

                // skip if the buffer is not enabled in the service
                if (itemBuffer is null)
                    continue;

                string path = itemBuffer.BufferPath ?? _conf.Buffer.BufferPath;
                if (!_dirHandlers.Any(x => x.path == new DirectoryInfo(path).FullName))
                    _dirHandlers.Add(new DirectoryHandler(path, item.Name,
                        ".buff", $"BufferService_{item.Name}")); // buffer file suffix & prefix
                else
                    _dirHandlers.First(x => x.path == new DirectoryInfo(path).FullName).AddNewServiceName(
                        item.Name, ".buff", $"BufferService_{item.Name}");
            }
            return Task.CompletedTask;
        }

        public override string? ToString()
        {
            return base.ToString();
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug("BufferHandlerService service is running.");
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            Stopwatch sw = new Stopwatch();

            // Buffer ->
            //Dictionary<(string, INetFlexorService), Queue<SyncDataBufferContainer>>
            var Buffer = (GetFlexorBuffer() as FlexorDataBuffer).GetWholeBuffer();

            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                try
                {
                    //_dirHandlers = lists of paths to buffer directories
                    //                                                Key          Value
                    //_dirHandlers.serviceNameDict[0] (Dictionary) = <serviceName, DirectoryHandlerObject>
                    //_dirHandlers.serviceNameDict[0].Value = Handles the data for the specific service
                    List<Task> tasks = new List<Task>();

                    // loop through all folder handlers
                    foreach (var item in _dirHandlers)
                    {
                        // loop through all services that are linked to the buffer
                        var dict = item.GetServiceDict();
                        foreach (var serviceDir in dict)
                        {
                            // Handle according to the execution format: Parallel or Sequence
                            switch (_conf.GetExecutionFormat())
                            {
                                case ExecutionFormat.Parallel:
                                    tasks.Add(Task.Run(() => CheckBufferStatusAsync(serviceDir, item)));
                                    break;
                                case ExecutionFormat.Sequence:
                                    await CheckBufferStatusAsync(serviceDir, item);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // wait for the buffer checks to finish
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                sw.Stop();
                // Try to keep the execution interval as close as possible
                // If the execution took longer than the interval, skip the delay
                //_logger.LogInformation($"Execution time: {sw.ElapsedMilliseconds} ms");
                if (interval > sw.ElapsedMilliseconds)
                    await Task.Delay((int)(interval - sw.ElapsedMilliseconds), stoppingToken);
            }
            _logger.LogCritical("BufferHandlerService crashed!");
        }

        /// <summary>
        /// Function to check the buffer status if it is over the allowed size in memory or on disk.
        /// </summary>
        /// <param name="serviceDir"></param>
        /// <param name="directoryHandler"></param>
        /// <returns></returns>
        private async Task CheckBufferStatusAsync(KeyValuePair<string, DirectoryHandlerObject> serviceDir,
            DirectoryHandler directoryHandler)
        {
            // serviceDir ->
            // Key = service name
            // Value = directory handler for that service

            long allowedMemoryBufferSize = _conf.ConvertSizeToBytes(_conf.Buffer.MemoryBufferSize);
            long allowedDiskBufferSize = _conf.ConvertSizeToBytes(_conf.Buffer.DiskBufferSize);
            long currentBufferSize = ReadServiceBufferSize(serviceDir.Key);
            if (currentBufferSize > allowedMemoryBufferSize)
            {
                while (currentBufferSize > allowedMemoryBufferSize)
                {
                    if (_conf.Buffer.BufferToDisk)
                    {
                        // Check if the disk buffer is full and delete old data if it is
                        if (BufferHandlerToolKit.PossiblyDeleteOldBufferData(directoryHandler, serviceDir.Value, currentBufferSize,
                            allowedDiskBufferSize, /*_conf.Buffer.BufferEachContainer*/ true))
                            _logger.LogWarning($"Deleted old data from the buffer folder! -> {serviceDir.Key}\n\tPlease check if out data is flowing correctly!");

                        // start to dump the buffer to disk
                        if (TryDequeueDataFromBuffer(serviceDir.Key, out var container))
                        {
                            if (container is not null)
                            {
                                if (_conf.Buffer.BufferEachContainer)
                                    BufferHandlerToolKit.WriteBufferDataToDisk(container, directoryHandler, serviceDir.Value);
                                else
                                    BufferHandlerToolKit.StreamContainerToFile(container, directoryHandler, serviceDir.Value);
                            }
                        }
                    }
                    else
                    {
                        if (TryDequeueDataFromBuffer(serviceDir.Key, out var container))
                            _logger.LogWarning($"Buffer for {serviceDir.Key} is full. Removing oldest data.");
                    }
                    currentBufferSize = ReadServiceBufferSize(serviceDir.Key);
                }
            }
            else
            {
                // Read disk buffer and add back to the memory buffer
                if (_conf.Buffer.BufferToDisk)
                {
                    var data = BufferHandlerToolKit.ReadBufferDataFromDisk(directoryHandler, serviceDir.Value,
                        allowedMemoryBufferSize, currentBufferSize);
                    foreach (var container in data)
                    {
                        EnqueueDataToServiceBuffer(container, serviceDir.Key);
                    }
                }
            }
        }
    }
}
