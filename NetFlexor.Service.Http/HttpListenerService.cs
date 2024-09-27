/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      HttpListenerService listens for incoming HTTP requests and processes the data.
 *      It also has increaded security features to prevent unwanted network traffic.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using NetFlexor.Interfaces;

namespace NetFlexor.Service.Http
{
    public class HttpListenerService : NetFlexorBaseService
    {
        HttpListenerServiceConfiguration _conf;
        ILogger<HttpListenerService> _logger;
        public HttpListenerService(IFlexorDataBuffer buffer, ILogger<HttpListenerService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as HttpListenerServiceConfiguration;
            //return base.InitializeAsync(conf, stoppingToken);
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("Http listener service is running.");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                try
                {
                    await Task.Run(() => StartHttpListener(_conf.ListeningUrl, stoppingToken), stoppingToken);
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
        /// <summary>
        /// Listens for incoming HTTP requests and processes the data.
        /// </summary>
        /// <param name="inputUrl"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task StartHttpListener(string inputUrl, CancellationToken stoppingToken)
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(inputUrl);
                try
                {
                    listener.Start();
                    _logger.LogInformation($"Listening for HTTP requests on {inputUrl}...");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        HttpListenerContext context = await listener.GetContextAsync().WaitAsync(stoppingToken);
                        Task.Run(async () => await HandleListenerContextInputDataAsync(context, stoppingToken), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in HTTP listener for {inputUrl}: {ex.Message}");
                }
                finally
                {
                    listener.Stop();
                }
            }
        }

        private async Task HandleListenerContextInputDataAsync(HttpListenerContext context, CancellationToken stoppingToken)
        {
            // Check if the source ip address is allowed
            if (!HttpHelper.IsSourceIpAddressAllowed(_conf.AllowedSources, context.Request.RemoteEndPoint.Address.ToString()))
            {
                _logger.LogWarning($"Request from {context.Request.RemoteEndPoint.Address} is not allowed by the configuration.");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.Close();
                return;
            }

            // Check if the http method is allowed
            if (!HttpHelper.IsHttpMethodAllowed(_conf.GetAllowedHttpMethods(), context.Request))
            {
                _logger.LogWarning($"Request method {context.Request.HttpMethod} is not allowed by the configuration.");
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.Close();
                return;
            }
            FlexorDataBufferContainer data = await RunListenerOpertaiotnsAsync(context.Request, context.Response, stoppingToken);
            await HandleHttpData(data, stoppingToken);
        }

        private async Task HandleHttpData(FlexorDataBufferContainer data, CancellationToken stoppingToken)
        {
            data.ServiceName = _conf.Name;
            EnqueueDataToBuffer(data);
        }
        private async Task<FlexorDataBufferContainer>? RunListenerOpertaiotnsAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken stoppingToken)
        {
            string requestBody = string.Empty;

            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            FlexorDataBufferContainer dataModel = null;
            try
            {
                //SyncDataBufferContainer
                dataModel = JsonSerializer.Deserialize<FlexorDataBufferContainer>(requestBody);

                response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON deserialization error: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await WriteResponseAsync(response, $"Invalid JSON format: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await WriteResponseAsync(response, $"Internal server error: {ex.Message}");
            }
            finally
            {
                response.Close();
            }

            return dataModel; // Or return 'data' if your method is supposed to return the deserialized object
        }

        private async Task WriteResponseAsync(HttpListenerResponse response, string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
