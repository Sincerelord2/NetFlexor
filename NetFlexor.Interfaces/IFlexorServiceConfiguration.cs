/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      The main configuraion class for the application.
 *      This contains the common and service configurations classes.
 *      It also contains the factory class for creating the service configurations out of the yaml configuration file.
 * 
 */

using System.Net;
using YamlDotNet.Serialization;

namespace NetFlexor.Interfaces
{
    public class FlexorServiceConfigurationFactory
    {
        public static IFlexorServiceConfiguration CreateService(string type)
        {
            return type switch
            {
                // Buffer configuration is not a service that user can create, it will always be created by the factory
                "Random.Service" => new RandomServiceConfiguration(),
                "File.Serializer.Service" => new FileSerializerServiceConfiguration(),
                "Http.Service" => new HttpServiceConfiguration(),
                "Http.Proxy.Service" => new HttpProxyServiceConfiguration(),
                "Http.Listener.Service" => new HttpListenerServiceConfiguration(),
                "Http.Api.Service" => new HttpApiServiceConfiguration(),
                "Tcp.Proxy.Service" => new TcpServiceConfiguration(),
                "Interface.Service" => new ApplicationInterfaceConfiguration(),
                // Add other service types here
                _ => throw new ArgumentException($"Unsupported service type: {type}"),
            };
        }
    }
    public interface IFlexorServiceConfiguration
    {
        string Name { get; set; }
        string GetTypeServiceName();
        //void PopulateCommonConfiguration(Common common);
        void PopulateNetFlexorConfiguration(NetFlexorConfiguration config);
        BufferConfiguration GetFlexorServiceBufferConfiguration();
    }

    public interface ITypeConfiguration
    {
        string Type { get; set; }
    }

    public interface IExecutionConfiguration
    {
        string ExecutionInterval { get; set; }
        string ExecutionFormatString { get; set; }
        long getExecutionInterval(timeUnit unit);
    }

    public abstract class FlexorServiceBaseConfiguration : IFlexorServiceConfiguration
    {
        /// <summary>
        /// Name of the service.
        /// </summary>
        public string Name { get; set; } = null;
        /// <summary>
        /// Path to the folder where the files will be stored.
        /// </summary>
        public string Folder { get; set; } = null;
        public string Url { get; set; } = null;
        public string Method { get; set; } = null;
        public string Accept { get; set; } = null;
        public string ContentType { get; set; } = null;
        [YamlIgnore]
        public string FullDirectoryPath
        {
            get
            {
                return new DirectoryInfo(Folder).FullName;
            }
        }
        /// <summary>
        /// Allowed folder size. <br></br>
        /// 0 = unlimited
        /// </summary>
        public string AllowedFolderSize { get; set; } = "500MB";

        /// <summary>
        /// Folder size in bytes.
        /// </summary>
        [YamlIgnore]
        public long allowedFolderSizeInBytes
        {
            get
            {
                return ConvertSizeToBytes(AllowedFolderSize);
            }
        }
        public long AllowedFileCount { get; set; } = 0;

        /// <summary>
        /// Prefix for the file name.
        /// </summary>
        public string FilePrefix { get; set; } = null;
        /// <summary>
        /// Suffix for the file name.
        /// </summary>
        public string FileSuffix { get; set; } = null;
        /// <summary>
        /// Data format for the input data.
        /// </summary>
        public string DataFormat { get; set; } = null;
        //public string DefaultDataFormat { get; set; } = null;
        public abstract string GetTypeServiceName();
        //private abstract void PopulateCommonConfiguration(Common common);
        public abstract void PopulateNetFlexorConfiguration(NetFlexorConfiguration config);
        public abstract BufferConfiguration GetFlexorServiceBufferConfiguration();

        public long ConvertSizeToBytes(string size)
        {
            if (string.IsNullOrWhiteSpace(size))
            {
                throw new ArgumentException("Size must be a non-empty string.");
            }

            // Trim the size string to remove any leading or trailing whitespaces
            size = size.Trim();

            // Extract the numeric part of the size
            var numericPart = new string(size.TakeWhile(char.IsDigit).ToArray());
            if (!long.TryParse(numericPart, out long number))
            {
                throw new ArgumentException("Invalid size format. Numeric part could not be parsed.");
            }

            // Extract the unit part of the size
            var unitPart = size.Substring(numericPart.Length).Trim().ToUpper();

            // Convert the size to bytes based on the unit
            return unitPart switch
            {
                "B" => number,
                "KB" => number * 1024L,
                "MB" => number * 1024L * 1024L,
                "GB" => number * 1024L * 1024L * 1024L,
                "TB" => number * 1024L * 1024L * 1024L * 1024L,
                "PB" => number * 1024L * 1024L * 1024L * 1024L * 1024L,
                _ => throw new ArgumentException($"Unsupported size unit: {unitPart}"),
            };
        }
    }

    public abstract class FlexorServiceCommonConfiguration : FlexorServiceBaseConfiguration
    {
        public string Type { get; set; }
        public string ExecutionInterval { get; set; } = "1s";
        public string[] AllowedSources { get; set; } = ["*.*.*.*/0"]; // Default to allow all sources

        public IPAddress[] GetAllowedIPAddresses()
        {
            List<IPAddress> ipAddresses = new();
            if (AllowedSources != null)
            {
                for (int i = 0; i < AllowedSources.Length; i++)
                {
                    var ip = AllowedSources[i].Split("/");
                    if (ip.Length < 2)
                        continue;
                    if (IPAddress.TryParse(ip[0], out IPAddress ipAddress))
                        ipAddresses.Add(ipAddress);
                }
            }
            return ipAddresses.ToArray();
        }

        [YamlMember(Alias = "ExecutionFormat")]
        public string ExecutionFormatString { get; set; } = null;

        public ExecutionFormat GetExecutionFormat()
        {
            if (ExecutionFormatString is null)
                return ExecutionFormat.Parallel;

            switch (ExecutionFormatString.ToLower())
            {
                case "sequence":
                    return ExecutionFormat.Sequence;
                case "parallel":
                default:
                    return ExecutionFormat.Parallel;
            }
        }
        public BufferConfiguration Buffer { get; set; } = null;
        public List<LinkedService> Linked { get; set; } = null;
        public long getExecutionInterval(timeUnit unit)
        {
            // Check if ExecutionInterval is null or empty
            if (string.IsNullOrEmpty(ExecutionInterval))
            {
                throw new InvalidOperationException("ExecutionInterval is not set.");
            }

            // Extract the numeric part of the ExecutionInterval
            var numericPart = new string(ExecutionInterval.TakeWhile(char.IsDigit).ToArray());
            if (!long.TryParse(numericPart, out long interval))
            {
                throw new InvalidOperationException("ExecutionInterval does not start with a valid number.");
            }

            // Determine the original unit from the suffix
            var unitSuffix = ExecutionInterval.Substring(numericPart.Length).ToLower();
            long intervalInMilliseconds = interval; // Default to milliseconds if no unit is found
            switch (unitSuffix)
            {
                case "ms":
                    break; // Already in milliseconds, no conversion needed
                case "s":
                    intervalInMilliseconds = interval * 1000; // Convert seconds to milliseconds
                    break;
                case "m":
                    intervalInMilliseconds = interval * 60000; // Convert minutes to milliseconds
                    break;
                case "h":
                    intervalInMilliseconds = interval * 3600000; // Convert hours to milliseconds
                    break;
                case "d":
                    intervalInMilliseconds = interval * 86400000; // Convert days to milliseconds
                    break;
                default:
                    throw new InvalidOperationException("ExecutionInterval has an unrecognized unit.");
            }

            // Convert from milliseconds to the desired unit
            switch (unit)
            {
                case timeUnit.ms:
                    return intervalInMilliseconds;
                case timeUnit.s:
                    return intervalInMilliseconds / 1000;
                case timeUnit.m:
                    return intervalInMilliseconds / 60000;
                case timeUnit.h:
                    return intervalInMilliseconds / 3600000;
                case timeUnit.d:
                    return intervalInMilliseconds / 86400000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), $"Not expected timeUnit value: {unit}");
            }
        }
    }

    // ------------------------------------------->
    // Define service configuration classes here ->
    // ------------------------------------------->

    /// <summary>
    /// Random number generator service configuration.
    /// </summary>
    public class RandomServiceConfiguration : FlexorServiceCommonConfiguration//, ITypeConfiguration, IExecutionConfiguration
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Count { get; set; }

        //[YamlMember(Alias = "ExecutionFormat")]
        //public string ExecutionFormatString { get; set; } = null;

        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            return this.Buffer;
        }
        private void PopulateCommonConfiguration(Common common)
        {
            if (this.Buffer is null)
            {
                this.Buffer = common.Buffer;
            }
            else
            {
                if (this.Buffer.BufferPath is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.BufferFile is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.MemoryBufferSize is null)
                    this.Buffer.MemoryBufferSize = common.Buffer.MemoryBufferSize;
                if (this.Buffer.DiskBufferSize is null)
                    this.Buffer.DiskBufferSize = common.Buffer.DiskBufferSize;
            }

            if (this.ExecutionInterval is null)
                this.ExecutionInterval = common.ExecutionInterval;
            if (this.ExecutionFormatString is null)
                this.ExecutionFormatString = common.ExecutionFormatString;
        }


        public override string GetTypeServiceName()
        {
            return "RandomService";
        }

        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            PopulateCommonConfiguration(config.Common);
        }
    }

    /// <summary>
    /// File serializer service configuration. <br></br>
    /// This will serialize data to a file in any user specified format or type.
    /// </summary>
    public class FileSerializerServiceConfiguration : FlexorServiceCommonConfiguration//, ITypeConfiguration, IExecutionConfiguration
    {
        // Similar explicit implementation as RandomServiceConfiguration
        // Add file serializer specific properties here if needed
        //[YamlMember(Alias = "ExecutionFormat")]
        //public string ExecutionFormatString { get; set; } = null;

        /// <summary>
        /// Returns the actual service name.
        /// </summary>
        /// <returns></returns>
        public override string GetTypeServiceName()
        {
            return "FileSerializerService";
        }
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            return this.Buffer;
        }
        private void PopulateCommonConfiguration(Common common)
        {
            if (this.Buffer is null)
            {
                this.Buffer = common.Buffer;
            }
            else
            {
                if (this.Buffer.BufferPath is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.BufferFile is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.MemoryBufferSize is null)
                    this.Buffer.MemoryBufferSize = common.Buffer.MemoryBufferSize;
                if (this.Buffer.DiskBufferSize is null)
                    this.Buffer.DiskBufferSize = common.Buffer.DiskBufferSize;
            }

            if (this.ExecutionInterval is null)
                this.ExecutionInterval = common.ExecutionInterval;
            if (this.ExecutionFormatString is null)
                this.ExecutionFormatString = common.ExecutionFormatString;
        }
        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            PopulateCommonConfiguration(config.Common);
        }
    }

    /// <summary>
    /// Tcp service configuration.
    /// </summary>
    public class TcpServiceConfiguration : FlexorServiceCommonConfiguration
    {
        public string Listening { get; set; } = null;
        public string Forward { get; set; } = null;
        public string KeepAlive { get; set; } = "10s";

        public long GetKeepAliveInSeconds()
        {
            // String format can be handled like the ExecutionInterval

            // Extract the numeric part of the KeepAlive
            var numericPart = new string(KeepAlive.TakeWhile(char.IsDigit).ToArray());
            if (!long.TryParse(numericPart, out long interval))
            {
                throw new InvalidOperationException("KeepAlive does not start with a valid number.");
            }

            // Determine the original unit from the suffix
            var unitSuffix = KeepAlive.Substring(numericPart.Length).ToLower();
            long returnInterval = interval; // Default to milliseconds if no unit is found
            switch (unitSuffix)
            {
                case "s":
                    //returnInterval = interval * 1000; // Convert seconds to milliseconds
                    break;
                case "m":
                    returnInterval = interval * 60000; // Convert minutes to milliseconds
                    break;
                case "h":
                    returnInterval = interval * 3600000; // Convert hours to milliseconds
                    break;
                case "d":
                    returnInterval = interval * 86400000; // Convert days to milliseconds
                    break;
                default:
                    throw new InvalidOperationException("KeepAlive has an unrecognized unit.");
            }

            return returnInterval;
        }
        public int GetListeningPort()
        {
            if (Listening is null)
                return 0;
            var port = Listening.Split(":");
            if (port.Length < 2)
                return 0;
            if (int.TryParse(port[1], out int portNumber))
                return portNumber;
            return 0;
        }
        public int GetForwardPort()
        {
            if (Forward is null)
                return 0;
            var port = Forward.Split(":");
            if (port.Length < 2)
                return 0;
            if (int.TryParse(port[1], out int portNumber))
                return portNumber;
            return 0;
        }
        public IPAddress GetListeningIpAddress()
        {
            if (Listening is null)
                return IPAddress.Any;
            var ip = Listening.Split(":");
            if (ip.Length < 2)
                return IPAddress.Any;
            if (IPAddress.TryParse(ip[0], out IPAddress ipAddress))
                return ipAddress;
            return IPAddress.Any;
        }
        public IPAddress GetForwardIpAddress()
        {
            if (Forward is null)
                return IPAddress.Any;
            var ip = Forward.Split(":");
            if (ip.Length < 2)
                return IPAddress.Any;
            if (IPAddress.TryParse(ip[0], out IPAddress ipAddress))
                return ipAddress;
            return IPAddress.Any;
        }
        public override string GetTypeServiceName()
        {
            return "TcpProxyService";
        }
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            return this.Buffer;
        }
        private void PopulateCommonConfiguration(Common common)
        {
            if (this.Buffer is null)
            {
                this.Buffer = common.Buffer;
            }
            else
            {
                if (this.Buffer.BufferPath is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.BufferFile is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.MemoryBufferSize is null)
                    this.Buffer.MemoryBufferSize = common.Buffer.MemoryBufferSize;
                if (this.Buffer.DiskBufferSize is null)
                    this.Buffer.DiskBufferSize = common.Buffer.DiskBufferSize;
            }

            if (this.ExecutionInterval is null)
                this.ExecutionInterval = common.ExecutionInterval;
            if (this.ExecutionFormatString is null)
                this.ExecutionFormatString = common.ExecutionFormatString;
        }

        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            PopulateCommonConfiguration(config.Common);
        }
    }

    /// <summary>
    /// Http service configuration. <br></br>
    /// For outputting data to a http endpoint.
    /// </summary>
    public class HttpServiceConfiguration : FlexorServiceCommonConfiguration
    {
        //public string Url { get; set; } = null;
        //public string ContentType { get; set; } = null;
        //public string Accept { get; set; } = null;
        //public string Method { get; set; } = null;
        public string[] AllowedHttpMethods { get; set; } = ["POST", "GET", "PUT", "DELETE", "PATCH"];
        public string DataFormat { get; set; } = null;
        public string OutputUrl { get; set; }

        /// <summary>
        /// Max count of containers that are send in every interval.
        /// </summary>
        public int ContainerCount { get; set; } = 5;

        [YamlMember(Alias = "HttpServiceType")]
        public string HttpServiceTypeString { get; set; } = null;

        /// <summary>
        /// Returns the allowed http methods as HttpMethod array.
        /// </summary>
        /// <returns></returns>
        public HttpMethod[] GetAllowedHttpMethods()
        {
            List<HttpMethod> httpMethods = new ();
            if (this.AllowedHttpMethods != null)
            {
                for (int i = 0; i < AllowedHttpMethods.Length; i++)
                {
                    var method = AllowedHttpMethods[i].ToLower() switch
                    {
                        "get" => HttpMethod.Get,
                        "post" => HttpMethod.Post,
                        "put" => HttpMethod.Put,
                        "delete" => HttpMethod.Delete,
                        "patch" => HttpMethod.Patch,
                        _ => HttpMethod.Post,
                    };

                    if (!httpMethods.Contains(method))
                        httpMethods.Add(method);
                }
            }
            else
                httpMethods.Add(HttpMethod.Post);
           
            return httpMethods.ToArray();
        }
        /// <summary>
        /// This is only used in the internal data http handler
        /// </summary>
        /// <returns></returns>
        public HttpServiceType GetHttpServiceType()
        {

           if (HttpServiceTypeString is null)
                return HttpServiceType.DataTransfer;
            return HttpServiceTypeString.ToLower() switch
            {
                "DataTransfer" => HttpServiceType.DataTransfer,
                "DataRead" => HttpServiceType.DataRead,
                _ => HttpServiceType.DataTransfer,
            };
        }
        public HttpMethod GetHttpMethod()
        {
            if (Method is null)
                return HttpMethod.Post;
            return Method.ToLower() switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => HttpMethod.Post,
            };
        }
        public override string GetTypeServiceName()
        {
            return "HttpService";
        }
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            return this.Buffer;
        }
        private void PopulateCommonConfiguration(Common common)
        {
            if (this.Buffer is null)
            {
                this.Buffer = common.Buffer;
            }
            else
            {
                if (this.Buffer.BufferPath is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.BufferFile is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                if (this.Buffer.MemoryBufferSize is null)
                    this.Buffer.MemoryBufferSize = common.Buffer.MemoryBufferSize;
                if (this.Buffer.DiskBufferSize is null)
                    this.Buffer.DiskBufferSize = common.Buffer.DiskBufferSize;
            }

            if (this.ExecutionInterval is null)
                this.ExecutionInterval = common.ExecutionInterval;
            if (this.ExecutionFormatString is null)
                this.ExecutionFormatString = common.ExecutionFormatString;
        }
        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            PopulateCommonConfiguration(config.Common);
        }
    }

    /// <summary>
    /// Http api service configuration.
    /// </summary>
    public class HttpListenerServiceConfiguration : HttpServiceConfiguration
    {
        //public string DataFormat { get; set; } = null;
        //public string OutputUrl { get; set; }
        public string TimeFormat { get; set; } = "unix-ms";
        public string ListeningUrl { get; set; }
        public override string GetTypeServiceName()
        {
            return "HttpListenerService";
        }
    }

    /// <summary>
    /// Http api service configuration.
    /// </summary>
    public class HttpApiServiceConfiguration : HttpListenerServiceConfiguration
    {
        public override string GetTypeServiceName()
        {
            return "HttpApiService";
        }
    }

    /// <summary>
    /// Http proxy service configuration.
    /// </summary>
    public class HttpProxyServiceConfiguration : HttpServiceConfiguration
    {
        //public string[] AllowedHttpMethods { get; set; }
        //public string OutputUrl { get; set; }
        public string ListeningUrl { get; set; }
        public override string GetTypeServiceName()
        {
            return "HttpProxyService";
        }
    }

    // <-------------------------------------------------
    // <- Define other service configuration classes here
    // <-------------------------------------------------

    /// <summary>
    /// Not implemented yet, but will be.
    /// </summary>
    public class ApplicationInterfaceConfiguration : FlexorServiceCommonConfiguration
    {
        //public string Url { get; set; } = null;
        /// <summary>
        /// Configuration used in the application, can be edited on the fly? dont know yet
        /// </summary>
        [YamlIgnore]
        public NetFlexorConfiguration mainConfiguration { get; set; } = null;
        public override string GetTypeServiceName()
        {
            return "ApplicationInterfaceService";
        }
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            return null;
        }

        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            mainConfiguration = config;
        }
    }

    /// <summary>
    /// Configuration for the linker micro service. <br></br>
    /// </summary>
    public class LinkedService : FlexorServiceBaseConfiguration
    {
        // Does not implement ITypeConfiguration or IExecutionConfiguration, so it doesn't have access to Type, ExecutionInterval, and ExecutionFormat
        // Add link specific properties here

        public override string GetTypeServiceName()
        {
            // TODO: make a check for "LinkService" when generating services since we don't have a specific type for this
            // This is only linker to other services
            return "LinkService";
        }
        /// <summary>
        /// Not implemented since linker does not have a buffer configuration
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            throw new NotImplementedException();
        }
        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            // do nothing since this service does not require this configuration
        }
    }

    public class BufferHandlerServiceConfiguration : FlexorServiceCommonConfiguration, ITypeConfiguration, IExecutionConfiguration
    {
        // Similar explicit implementation as RandomServiceConfiguration
        // Add file serializer specific properties here if needed
        public List<IFlexorServiceConfiguration> serviceConfigurations = new();
        [YamlMember(Alias = "ExecutionFormat")]
        public string ExecutionFormatString { get; set; } = null;

        public BufferHandlerServiceConfiguration()
        {
            Type = "BufferHandlerService";
        }
        /// <summary>
        /// Not implemented since this is the service that calls this function
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override BufferConfiguration GetFlexorServiceBufferConfiguration()
        {
            throw new NotImplementedException();
        }
        public void AddServiceConfiguration(IFlexorServiceConfiguration serviceConfiguration)
        {
            serviceConfigurations.Add(serviceConfiguration);
        }
        public override string GetTypeServiceName()
        {
            return Type;
        }
        private void PopulateCommonConfiguration(Common common)
        {
            if (this.Buffer is null)
                this.Buffer = common.Buffer;
            else
            {
                if (this.Buffer.BufferPath is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                else if (this.Buffer.BufferFile is null)
                    this.Buffer.BufferPath = common.Buffer.BufferPath;
                else if (this.Buffer.MemoryBufferSize is null)
                    this.Buffer.MemoryBufferSize = common.Buffer.MemoryBufferSize;
                else if (this.ExecutionInterval is null)
                    this.ExecutionInterval = common.ExecutionInterval;
                else if (this.ExecutionInterval is null)
                    this.ExecutionInterval = common.ExecutionInterval;
                else if (this.Buffer.DiskBufferSize is null)
                    this.Buffer.DiskBufferSize = common.Buffer.DiskBufferSize;
            }
        }
        public override void PopulateNetFlexorConfiguration(NetFlexorConfiguration config)
        {
            PopulateCommonConfiguration(config.Common);
        }
    }

    /// <summary>
    /// Common yaml parameters for all services. <br></br>
    /// Can be overridden by individual services.
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Buffer properties
        /// Can be overwridden by individual services within their configuration.
        /// </summary>
        [YamlMember(Alias = "Buffer")]
        public BufferConfiguration Buffer { get; set; } = new();
        /// <summary>
        /// Execution interval for all services. <br></br>
        /// Can be overwridden by individual services within their configuration.
        /// </summary>

        [YamlMember(Alias = "ExecutionInterval")]
        public string ExecutionInterval { get; set; } = "1s";
        /// <summary>
        /// Execution format for all services. <br></br>
        /// Can be overwridden by individual services within their configuration.
        /// </summary>

        [YamlMember(Alias = "ExecutionFormat")]
        public string ExecutionFormatString { get; set; } = "parallel";

        public ExecutionFormat getExecutionFormat
        {
            get
            {
                switch (ExecutionFormatString.ToLower())
                {
                    case "sequence":
                        return ExecutionFormat.Sequence;
                    case "parallel":
                    default:
                        return ExecutionFormat.Parallel;
                }
            }
        }
    }

    public class BufferConfiguration
    {
        // Common properties with default values
        /// <summary>
        /// Size of the allowed memory buffer
        /// </summary>
        [YamlMember(Alias = "MemoryBufferSize")]
        public string MemoryBufferSize { get; set; } = "10MB";
        /// <summary>
        /// Size of the allowed disk buffer. <br></br>
        /// Overflow will be lost if the buffer is full.
        /// </summary>

        [YamlMember(Alias = "DiskBufferSize")]
        public string DiskBufferSize { get; set; } = "10MB";
        /// <summary>
        /// Allow buffering to disk. If false, the buffer will be in memory only and overflow will be lost.
        /// </summary>

        [YamlMember(Alias = "BufferToDisk")]
        public bool BufferToDisk { get; set; } = true;
        /// <summary>
        /// Location of the buffer.
        /// </summary>

        [YamlMember(Alias = "BufferPath")]
        public string BufferPath { get; set; } = "/tmp/NetFlexor.Buffer";
        /// <summary>
        /// Base name of the buffer file. Each service will name the file with their own prefix.
        /// </summary>

        [YamlMember(Alias = "BufferFile")]
        public string BufferFile { get; set; } = "BufferFile";
        /// <summary>
        /// Enable single file buffer or create a new file for each request or container.
        /// </summary>
        //[YamlMember(Alias = "BufferEachContainer")]
        [YamlIgnore]
        public bool BufferEachContainer { get; set; } = true;

        [YamlMember(Alias = "BufferEnqueuePatchSize")]
        public uint BufferEnqueuePatchSize { get; set; } = 5;
    }

    public class NetFlexorConfiguration
    {
        public Common Common { get; set; }
        [YamlMember(Alias = "Services")]
        public List<Dictionary<string, object>> ServiceDictionaries { get; set; }

        [YamlIgnore]
        public List<IFlexorServiceConfiguration> getServicesConfiguration => ParseServices();
        [YamlIgnore]
        public List<IFlexorServiceConfiguration>? Services;

        /// <summary>
        /// Call this function to generate the service configuration to the list
        /// </summary>
        public void GenerateServiceConfiguration()
        {
            Services = getServicesConfiguration;
        }

        private List<IFlexorServiceConfiguration> ParseServices()
        {
            var services = new List<IFlexorServiceConfiguration>();
            var bufferHandler = new BufferHandlerServiceConfiguration();
            foreach (var serviceDict in ServiceDictionaries)
            {
                if (serviceDict.TryGetValue("Type", out var typeObj) && typeObj is string type)
                {
                    IFlexorServiceConfiguration service = FlexorServiceConfigurationFactory.CreateService(type);
                    var deserializer = new DeserializerBuilder().Build();
                    var yaml = new SerializerBuilder().Build().Serialize(serviceDict);
                    service = (IFlexorServiceConfiguration)deserializer.Deserialize(yaml, service.GetType());
                    bufferHandler.AddServiceConfiguration(service);
                    services.Add(service);
                }
            }
            services.Add(bufferHandler);
            return services;
        }
    }
}
