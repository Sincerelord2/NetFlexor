/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This is the main program file for the NetFlexor application.
 *      It will import the configuration file and append the services to the host.
 *      At the end it will run the host.
 * 
 */

using NetFlexor;
using NetFlexor.Factory;
using NetFlexor.Interfaces;
using NetFlexor.Service.Tcp;
using NetFlexor.Service.Http;
using NetFlexor.Service.Interface;
using NetFlexor.Service.BufferHandler;
using NetFlexor.Service.FileSerializer;
using NetFlexor.Service.RandomServiceObject;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

string softwareVersion = "1.0.0-prerelease";

// Handle arguments if there are any
foreach (string arg in args)
{
    switch (arg.ToLower())
    {
        case "-h":
        case "--help":
            ShowHelp();
            break;
        case "-v":
        case "--version":
            ShowVersion(softwareVersion);
            break;
        default:
            Console.WriteLine($"Bad argument: {arg}");
            Environment.Exit(0xA0);
            break;
    }
}

var host = Host.CreateDefaultBuilder(args);

host.ConfigureServices((context, services) =>
{
    //services.AddSingleton<SeviceFactory>();
    services.AddSingleton<IServiceCollection>(services);
    services.AddSingleton<FlexorServiceFactory>();
    services.AddSingleton<IFlexorDataBuffer, FlexorDataBuffer>();

    // Buffer handler service
    services.AddTransient<BufferHandlerService>();


    // Configurable services ->

    // Random number generator service
    services.AddTransient<RandomService>();

    // Application interface service for third party applications and future web interface
    // User can enable this interface if needed
    // NOTE: Not implemented yet
    services.AddTransient<ApplicationInterfaceService>();

    // File serializer service i.e. File output service
    services.AddTransient<FileSerializerService>();

    // Http data push service
    services.AddTransient<HttpService>();

    // Http proxy service (No internal buffer data handler implemented)
    // Http specific proxy and no other protocols supported
    services.AddTransient<HttpProxyService>();

    // Http api service with data transformation and buffering
    services.AddTransient<HttpListenerService>();

    // Tcp proxy service (No internal buffer data handler implemented)
    // This service can be used like a reverse proxy for tcp connections
    // Like HTTP and other protocols
    services.AddTransient<TcpProxyService>();

    // <- Configurable services

    //var conf = context.Configuration.ConfigurationFile;
    services.AddOptions<ApplicationOptions>().Bind(context.Configuration).ValidateOnStart();

    services.AddSingleton<FlexorServiceRunner>();

    services.AddHostedService<FlexorServiceRunner>(x => x.GetRequiredService<FlexorServiceRunner>());
});

var hostBuilder = host.Build();

await hostBuilder.RunAsync();


static void ShowHelp()
{
    Console.WriteLine("");
    Console.WriteLine("Usage: appname [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -h, --help       Show help information");
    Console.WriteLine("  -v, --version Show version information");

    // end execution to prevent further execution
    Environment.Exit(0);
}

static void ShowVersion(string version)
{
    Console.WriteLine($"NetFlexor current version: {version}");

    // end execution to prevent further execution
    Environment.Exit(0);
}