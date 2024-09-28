# NetFlexor 1.0.0-prerelease

Hello, amateur coder here.

This project is purely created to learn and understand coding in C# and .NET with the help of an AI.
I already have some experience with C# and .NET from school and work, but I wanted to learn more so I decided to create this project on my free time.

NetFlexor is a hobby based project to forward trafic to known http or tcp ports/endpoints.
It can offer a simple way to forward trafic to a specific endpoint, or to a specific port.
User can configure allowed methods, ip addresses, ports to limit the access to the forwarder service.

Other features are based on data handling like receiving a data package through http listener and inserting it to internal memory.
Buffering features will allow to store the data for a while, but it is recomended to generate files or forward the data known http endpoint.

NetFlexor is coded to be modular service based application. It has background services that can be added or removed based on the configuration.
Configuration is done through the config.yml file and it is imported to the application through appsettings.json file.

*Data collection features will be added in the future.*
*Supported data sources will be added based on the feedback, but few common sources will be added in the future like Modbus TCP/RTU and HTTP query builder*

    NOTE: Currently the project is in the development phase and it is not recomended to use it in production environment.
    All of the features have been tested once, but require long term testing to be sure that they are working as intended.

# Table of contents

- [Libraries and versions](#libraries-and-versions)
- [Services](#services)
- [Installation](#installation)
- [Usages](#usages)
  - [Configuration file (config.yml)](#configuration-file-configyml)
  - [Data buffering](#data-buffering)
  - [Service configuration](#service-configuration)
    - [Random data generator service](#random-data-generator-service-randomservice)
    - [HTTP Proxy Service](#http-proxy-service-httpproxyservice)
    - [TCP Proxy Service](#tcp-proxy-service-tcpproxyservice)
    - [HTTP Listener Service](#http-listener-service-httplistenerservice)
    - [File generation Service](#file-generation-service-fileserializerservice)
    - [HTTP Service](#http-service-httpservice)
- [Future](#future)
- [Contributing](#contributing)
- [License](#license)


# Libraries and versions

#### .NET 8.0

| Library name                              | Version | License | Author        |
|-------------------------------------------|---------|---------|---------------|
| Microsoft.Extensions.DependencyInjection  | 8.0.0   | MIT     | Microsoft     |
| Microsoft.Extensions.Hosting              | 8.0.0   | MIT     | Microsoft     |
| Microsoft.Extensions.Logging              | 8.0.0   | MIT	    | Microsoft     |
| Microsoft.Extensions.Logging.Abstractions | 8.0.1   | MIT	    | Microsoft     |
| System.Text.Json                          | 8.0.4   | MIT	    | Microsoft     |
| [Jsonata.Net.Native](https://github.com/aaubry/YamlDotNet/wiki) | 2.7.0   | MIT	    | Mikhail Barg  |
| [YamlDotNet](https://github.com/mikhail-barg/jsonata.net.native) | 16.1.3  | MIT	    | Antoine Aubry |

# Services

There three types of action a service can support: Forward, Data input and Data output.
The following table will show the features and the supported actions.

**Forward** action will not store the data, but will forward it to a new destination.
There is no data buffering or data storage, so that the client/sender should be aware of the data loss or buffer the data by itself.

**Input** action will store the data in the internal memory.
The data will be stored for a while, but it is recomended to generate files or forward the data to a known http endpoint.

**Output** action will read the internal memory and do the specific action that the feature is designed for i.e. generate files or forward the data to a known http endpoint.

The following table will show the features and the supported actions.
Detailed description of the features and how to use them will be in the [Usages](#usages) section.

| Service | Forward | Input | Output | Description |
| --- | --- | --- | --- | --- |
| Random data generator service | O | X | O | Generate random data and store it in the internal memory |
| HTTP Proxy Service | X | O | O | Forward http trafic to a new destination | 
| TCP Proxy Service | X | O | O | Forward tcp trafic to a new destination. Will keep the outgoing TCP connection alive specified amount of time after the last connection has been recieved in the input |
| HTTP Listener Service | O | X | O | Listen to incoming http traffic and store the data in internal memory |
| File generation Service | O | O | X | Generate files based on the received data |
| HTTP Service | O | O | X | Reads the internal memory and sends the data in user specified format to the defined http endpoint |

    You can have more than one service of the same type, but they must have different names and configurations.
    I.e. you can have multiple TCP proxies with different listening and forwarding ports.

# Installation

To install the NetFlexor application, you need download the NetFlexor.zip from the release assets.

# Usages

NetFlexor can be used as proxy service for http and tcp trafic. It can also be used as a data collector and forwarder.
The advantage of using NetFlexor is that it can be configured to allow only specific ip addresses, ports and http methods.
It can enhance the security of the network by limiting the access to the http or tcp endpoints.

## Configuration file (config.yml)

This is the main configuration file for the NetFlexor application.
The configuration file is in the YAML format and it is imported to the application through the appsettings.json file.

The configuration file is divided into two main sections: Common and Services.
Common section is used to configure the global settings for the configured services.

Below is the example of the common configuration for execution:
```yaml
Common:
...
  ExecutionInterval: 1s # Execution interval for the services can be in milliseconds, seconds, minutes, hours or days
                        # 1ms, 1s, 1m, 1h, 1d
                        # NOTE: You cannot mix time units with other units
                        # i.e. 1m 1s is not allowed and will throw an error.
  ExecutionFormat: parallel 
                        # Execute the service actions in parallel or in sequence
                        # parallel: all actions are executed at the same time
                        # sequence: all actions are executed one after another
                        # NOTE: this also affects the buffer service
...
```

### Data buffering

Buffering is a feature that allows to store the data in the internal memory for a while.
The data can be stored for a while, but it is recomended to generate files or forward the data to a known http endpoint.

Buffering can be configured to be individual for each service, but it can also be configured to be global for all services.
Only input capable services can buffer the data and will take advantage of the buffering feature.

Below is the example of the common buffer configuration:
```yaml
Common:
...
  Buffer:
    MemoryBufferSize: 10MB      # Buffer size
    DiskBufferSize: 1GB         # Buffer size
    BufferToDisk: true          # Wrtie memory buffer to disk
    BufferPath: flexorBuffer    # Buffer path
    BufferFile: buffer          # Buffer file
    BufferEnqueuePatchSize: 5   # How many containers are enqueued to the memory buffer at once from the disk buffer
                                # This is used to limit the internal memory usage when data flow is restored
...
```

### Service configuration

Each service is configured individually, but they all share the common configuration.
*Common configuration can be overwritten by the service configuration.*

#### Available services:
| Name | Configuration |
| --- | --- |
| Random data generator service | Random.service |
| HTTP Proxy Service | Http.proxy.service |
| TCP Proxy Service | Tcp.proxy.service |
| HTTP Listener Service | Http.listener.service |
| File generation Service | File.serializer.service |
| HTTP Service | Http.service |

#### Linkable services

Linkable services are only with the option to be an input for the internal buffer and output services can use the linked services as a data source.
Those services currently are: Random.Service and Http.listener.service.
Services than can currently use the linked services are: File.Serializer.Service and Http.Service.

```yaml
Services:
...
  - Type: File.Serializer.Service
    # Other configuration data
...
    Linked: # This means that the File.Serializer.Service will use the linked service as a data source
    - Name: linkedServiceName_1
      DataFormat: |
        jsonata:
        $.DataContainer.Data
      # DataFormat defines how the data will be written to the file
      path: /tmp/netFlexor
      AllowedFolderSize: 500MB
      AllowedFileCount: 10
      # With the above configuration, the File.Serializer.Service will use the linked service as a data source
      # and will write the data to the file with the defined format.
      # The data will be written to the /tmp/netFlexor folder and the folder size will be limited to 500MB or to 10 files.
```

Linked service can have individual configuration or it can use the service configuration.
I.e. if the File.Serializer.Service has the same configuration as the linked service, then the linked service configuration will be used.

#### Random data generator service [Random.Service]

This service is used to generate random data and store it in the internal memory.
It can be used to demonstrate or test the output features of the NetFlexor.
##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| Min | Int | Minimium number that can be generated |
| Max | Int | Maximium number that can be generated |
| Count | Int | Count of numbers how many to generate |
##### Example
```yaml
Services:
...
# This is an example of the random data generator service
  - Type: Random.Service
    Name: randomService_1
    Min: 1
    Max: 100
    Count: 10
...
```
#### HTTP Proxy Service [Http.proxy.service]

This service is used to forward http trafic to a new destination.
It will not store the data in the internal memory, so there is no data buffering enabled for this service.

Basic function is that it will listen to the incoming http trafic and when the request is received, it will open a new http request to the defined endpoint.
Within that request, it will open a stream to the endpoint and forward the data to the endpoint.

    NOTE: Only one connection is allowed at a time.
    Support for multiple connections will be added in the future.

##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| ListeningUrl | String | Url where the service will listen to the incoming http trafic |
| OutputUrl | String | Url where the service will forward the http trafic |
| AllowedSources | String array | List of allowed ip addresses |
| AllowedHttpMethods | String array | List of allowed http methods |
##### Example
```yaml
Services:
...
  - Type: Http.Proxy.Service
    Name: httpProxy_1
    ListeningUrl: http://192.168.1.10:8080/
    OutputUrl: http://192.168.56.10:1880/api/v2/write?
    AllowedSources: ["*.*.*.*/0"]                       # "*.*.*.*/0" means that all ip addresses are allowed
                                                        # ".../0" is the mask for the ip address
    AllowedHttpMethods: [POST,GET]                      # Allowed http methods
...
```

#### TCP Proxy Service [Tcp.proxy.service]

This service is just like the HTTP Proxy Service, but it is used to forward tcp trafic to a new destination.
In that sense it also can be used to forward http trafic, but it is not recomended to use it for that purpose.

    NOTE: Only one connection is allowed at a time.
    Support for multiple connections will be added in the future.

##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| Listening | String | Url where the service will listen to the incoming http trafic |
| Forward | String | Url where the service will forward the http trafic |
| AllowedSources | String array | List of allowed ip addresses |
| KeepAlive | String | Forward tcp connection keepalive time from the last connection in the input |
##### Example
```yaml
Services:
...
  - Type: Tcp.Proxy.Service
    Name: tcpProxy_1
    Listening: 127.0.0.1:3020
    Forward: 192.168.56.10:1880
    AllowedSources: ["*.*.*.*/0"]
    KeepAlive: 5s                   # How long to wait untill we close the Forward connection after the last connection has been recieved from the client
...
```

#### HTTP Listener Service [Http.listener.service]

This service is used to listen to incoming http trafic and store the data in the internal memory.
The data can be given to other services to be processed with next steps.

##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| ListeningUrl | String | Url where the service will listen to the incoming http trafic |
| AllowedSources | String array | List of allowed ip addresses |
| AllowedHttpMethods | String array | List of allowed http methods |
| TimeFormat | String | Timestamp format for the data |
| ContentType | String | Content type of the data |
##### Example
```yaml
Services:
...
  - Type: Http.Listener.Service
    Name: httpListener_1
    ListeningUrl: http://127.0.0.1:3030/
    AllowedSources: ["*.*.*.*/0"]
    AllowedHttpMethods: [POST,GET]
    TimeFormat: unix-s              # Optional, can be told to the service with the data
    ContentType: application/json
...
```
##### Json data format that can be sent to the listener
```json
{
  "Name": "httpListener_1",
  "DataContainer": [
    {
      "TimeFormat": "unix-s",
      "TimeStamp": 1630512000,
      "Data": [
        {
          "Name": "data_1",
          "Value": 2.1
        },
        {
          "Name": "data_2",
          "Data": 54.2
        }
      ]
    }
  ]
}
```

```"Name": "httpListener_1",``` Optional, but will be overwriten by the service.

```"DataContainer": [``` Data container with that specific timestamp

```"TimeFormat": "unix-s",``` Must tell the service how the timestamp is formatted. Supported: unix-s, unix-ms, unix-us, unix-ns, unix-ps, iso8601

    Note: if unix timestamp is in high precision than ms, then some data can be lost in the conversion.

```"TimeStamp": 1630512000,``` Timestamp of the data

```"Data": [``` Data array

```"Name": "data_1",``` Name of the data

```"Value": 2.1``` Data (type of object in C# so no need to define the type here)

#### File generation Service [File.Serializer.Service]

This service can take the input data from other services via the internal buffer and generate files based on the received data.
Files can be generated in different formats. Default format is JSON and it is the same format as the http listener input data.

User can define its own DataFormat how the data will be written to the file via NetFlexor specific format or with jsonata conversion.

##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| Folder | String | Folder path where the files are generated |
| FilePrefix | String | Prefix for the generated files |
| FileSuffix | String | Suffix for the generated files |
| ExecutionInterval | String | Execution interval for the service |
| Linked | Linked service array | List of services that are linked to this service |

##### Example
```yaml
Services:
...
  - Type: File.Serializer.Service
    Name: fileSerializer_1
    Folder: /tmp/terrasync  # Folder path
    FilePrefix: test        # File prefix name
    FileSuffix: .csv        # File suffix name. If DataFormat is empty, then the file suffix will be used as file format source
    ExecutionInterval: 5s   # Empty the buffer to file every 5 seconds
    Linked:
...
# This is an example of the linked input service
    - Name: randomNumber_1
      DataFormat: |
        jsonata:
        $.DataContainer.Data
      AllowedFolderSize: 500MB
      AllowedFileCount: 10
...
```

*Linked services are explained in the [Linkable services](#linkable-services) section.*

#### HTTP Service [Http.service]

This is an output service that can format the data into user specified format and send it to the defined http endpoint.
User can define its own DataFormat how the data will be sent to the endpoint via NetFlexor specific format or with jsonata conversion.

##### Configuration
| Name | Value | Description |
| --- | --- | --- |
| Name | String | Name of the service |
| Url | String | Url where the service will send the data |
| Method | String | Http method that will be used to send the data |
| ContentType | String | Content type of the data |
| ContainerCount | UInt | Max count of data containers that are send in every interval |
| ExecutionInterval | String | Execution interval for the service (If not defiend, then common execution interval is used) |
| Linked | Linked service array | List of services that are linked to this service |

##### Example
```yaml
Services:
...
  - Type: Http.Service
    Name: httpService_1
    Method: POST
    Accept: application/json
    ContentType: application/json
    ContainerCount: 5
    # Optional buffer configuration (Common settings are used if not defined):
  #   Buffer:
  #     MemoryBufferSize: 10MB
  #     DiskBufferSize: 10MB
  #     BufferToDisk: true
  #     BufferPath: /tmp/netFlexor/httpService
  #     BufferFile: httpBuffer
    Linked:
    - Name: httpListener_1
      Url: http://192.168.56.10:1880/api/v2/write?
      DataFormat: |
        jsonata:
        $.DataContainer.Data
```

The above configuration will send the data from the httpListener_1 service to the defined endpoint in the json format.

## Future

In the future, the NetFlexor will be expanded to support more services and data sources.
The goal is to make the NetFlexor a modular service based application that can be used to forward trafic and collect data from different sources.

### Roadmap:
  
- Multiconnection support for the proxy services (http and tcp)
- gRPC interface service to enable remote configuration of the NetFlexor application
- Blazor UI that takes advantage of the interface service and enables the user to configure the NetFlexor application
   - Will be its own application that will connect to the NetFlexor application through the gRPC interface service
   - Can be run on the same machine or on a different machine
- Modbus TCP/RTU input service
- Better security features
- Http input service that can request data from a known http endpoint (I.e any sql database that has http endpoint)
- Support for running scripts and using the console output as a data source

## Contributing

I am not accepting any contributions at the moment, but this might change in the future based on the popularity of the project.

## License

MIT License

Copyright (c) 2024 Sincerelord2

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
