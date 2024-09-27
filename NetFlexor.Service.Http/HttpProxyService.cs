/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Http proxy service is a service that listens to incoming HTTP requests and forwards them to the specified output URL.
 *      User can configure the allowed sources, allowed HTTP methods, and the listening URL.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using NetFlexor.Interfaces;

namespace NetFlexor.Service.Http
{
    // TODO: Make the HttpProxyService more memory efficient?
    //       There might be a better way to implement this service?
    public class HttpProxyService : NetFlexorBaseService
    {
        HttpProxyServiceConfiguration _conf;
        ILogger<HttpProxyService> _logger;
        public HttpProxyService(IFlexorDataBuffer buffer, ILogger<HttpProxyService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            _conf = conf as HttpProxyServiceConfiguration;
            //return base.InitializeAsync(conf, stoppingToken);
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("Http proxy service is running.");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                sw.Restart();
                try
                {
                    await Task.Run(() => StartListener(_conf.ListeningUrl, _conf.OutputUrl, stoppingToken), stoppingToken);
                    //await HttpProxyServiceRunner(stoppingToken);
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
        //private async Task HttpProxyServiceRunner(CancellationToken stoppingToken)
        //{
        //    // Do a single listening address at once because if the user wants multiple listening addresses, then they can create new proxy service
        //    await Task.Run(() => StartListener(_conf.ListeningUrl, _conf.OutputUrl, stoppingToken), stoppingToken);

        //    // Assuming _conf is your configuration object that contains InputUrls and OutputUrls
        //    //var inputUrl = _conf.ListeningUrl; // Your input URLs
        //    //var outputUrl = _conf.OutputUrl; // Your output URLs

        //    //// Validate that input and output URLs are correctly configured
        //    //if (inputUrl.Length != outputUrl.Length)
        //    //{
        //    //    _logger.LogError("The number of input URLs does not match the number of output URLs.");
        //    //    return;
        //    //}

        //    //List<Task> listenerTasks = new List<Task>();

        //    //for (int i = 0; i < inputUrls.Length; i++)
        //    //{
        //    //    var index = i; // Local copy for closure
        //    //    var listenerTask = Task.Run(() => StartListener(inputUrls[index], outputUrls[index], stoppingToken), stoppingToken);
        //    //    listenerTasks.Add(listenerTask);
        //    //}
        //    // Wait for all listeners to complete (this will likely run indefinitely until cancellation is requested)
        //    //await Task.WhenAll(listenerTasks);
        //}

        private async Task StartListener(string inputUrl, string outputUrl, CancellationToken stoppingToken)
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add(inputUrl);
                try
                {
                    listener.Start();
                    _logger.LogInformation($"Listening for HTTP Reverse Proxy requests on {inputUrl}...");

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        HttpListenerContext context = await listener.GetContextAsync().WaitAsync(stoppingToken);
                        //HttpListenerContext context = await listener.GetContextAsync();
                        //Task.Run(async () => await ForwardRequest(context, outputUrl, stoppingToken), stoppingToken);
                        await HandleHttpRequest(context, outputUrl, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in HTTP listener for {inputUrl}: {ex.Message}");
                }
                finally
                {
                    listener.Stop();
                    //listener.Close();
                }
            }
        }

        private async Task HandleHttpRequest(HttpListenerContext context, string outputUrl, CancellationToken stoppingToken)
        {
            // Check if the source ip address is allowed
            if (!HttpHelper.IsSourceIpAddressAllowed(_conf.AllowedSources, context.Request.RemoteEndPoint.Address.ToString()))
            {
                _logger.LogWarning($"Request from {context.Request.RemoteEndPoint.Address} is not allowed.");
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.Close();
                return;
            }

            // Check if the http method is allowed
            if (!HttpHelper.IsHttpMethodAllowed(_conf.GetAllowedHttpMethods(), context.Request))
            {
                _logger.LogWarning($"Request method {context.Request.HttpMethod} is not allowed by configuration.");
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.Close();
                return;
            }

            await RunProxyOpertaiotns(context.Request, context.Response, outputUrl, stoppingToken);
        }

        private async Task RunProxyOpertaiotns(HttpListenerRequest request, HttpListenerResponse response, string outputUrl, CancellationToken stoppingToken)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestMessage = new HttpRequestMessage(new HttpMethod(request.HttpMethod), outputUrl)
                {
                    Content = new StreamContent(request.InputStream)
                };

                // Copy headers from the incoming request to the outgoing request
                foreach (var header in request.Headers.AllKeys)
                {
                    if (!WebHeaderCollection.IsRestricted(header))
                        requestMessage.Headers.TryAddWithoutValidation(header, request.Headers[header]);
                }

                HttpResponseMessage result = await client.SendAsync(requestMessage, stoppingToken);

                // Correctly forward the status code
                response.StatusCode = (int)result.StatusCode;

                // Forward headers and content from the output URL server's response
                foreach (var header in result.Headers)
                {
                    response.AddHeader(header.Key, string.Join(",", header.Value));
                }

                using (var responseStream = await result.Content.ReadAsStreamAsync())
                {
                    await responseStream.CopyToAsync(response.OutputStream, stoppingToken);
                }
            }
            response.Close();
        }
    }
}
