/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Use case 1:
 *          This Http service can create http requests with the data from the buffer.
 *          User can define the http request type, url, headers, and body format.
 *      Use case 2: (not implemented yet -> tbd)
 *          User can define custom http request to remote server and get and parse the response to the buffer.
 *          The response can be used to create new requests base on the response content like timestamps or other data.
 *          These needs to be specified in the configuration file what data to use from the response.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using NetFlexor.Interfaces;
using NetFlexor.Data.Transformation;

namespace NetFlexor.Service.Http
{
    public class HttpService : NetFlexorBaseService
    {
        HttpServiceConfiguration _conf;
        ILogger<HttpService> _logger;
        public HttpService(IFlexorDataBuffer buffer, ILogger<HttpService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as HttpServiceConfiguration;
            //return base.InitializeAsync(conf, stoppingToken);
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("Http service is running.");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                // always skip the first run since there cannot be new data
                sw.Restart();
                try
                {
                    List<Task> tasks = new List<Task>();
                    var exFormat = _conf.GetExecutionFormat();
                    foreach (var item in _conf.Linked)
                    {
                        switch (exFormat)
                        {
                            case ExecutionFormat.Parallel:
                                tasks.Add(Task.Run(() => HttpServiceRunnerAsync(item)));
                                break;
                            case ExecutionFormat.Sequence:
                                await HttpServiceRunnerAsync(item);
                                break;
                            default:
                                break;
                        }
                    }
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
        }

        private async Task HttpServiceRunnerAsync(LinkedService linked)
        {
            switch (_conf.GetHttpServiceType())
            {
                case HttpServiceType.DataRead:
                    break;
                case HttpServiceType.DataTransfer:
                default:
                    await HttpDataTransferAsync(linked);
                    break;
            }
        }

        private async Task HttpDataTransferAsync(LinkedService linked)
        {
            var dataformat = linked.DataFormat ?? _conf.DataFormat;
            //var url = _conf.Url;
            var url = linked.Url ?? _conf.Url;

            if (url is null)
                throw new Exception("Url is not defined in the configuration file.");

            string httpContent = "";
            var http = new HttpClient();
            int containerCount = 0;

            while (TryDequeueDataFromBuffer(linked.Name, out var data))
            {
                httpContent = TransformDataFormat.GeneralFormatParser(dataformat, data);
                var content = new StringContent(httpContent, Encoding.UTF8, _conf.ContentType);

                if (httpContent.Length > 0)
                {
                    // start the http request
                    var res = http.SendAsync(new HttpRequestMessage
                    {
                        Method = _conf.GetHttpMethod(),
                        RequestUri = new Uri(url),
                        Content = content
                    }).Result;

                    if (!res.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Http request failed: {res.StatusCode}");
                        // add the data back to the buffer
                        EnqueueDataToServiceBuffer(data, linked.Name);
                        break; // break and continue to the next linked service
                    }
                    else
                        _logger.LogDebug($"Http status code: {res.StatusCode}");
                    containerCount++;
                }
                if (containerCount > _conf.ContainerCount)
                    break;
            }
        }
    }
}
