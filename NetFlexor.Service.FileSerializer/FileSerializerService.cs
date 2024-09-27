/*
 * 
 * Author: Sincerelord2
 * 
 * Copying and/or modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This service is responsible for serializing the data from the buffer to files.
 *      The service is configurable with the yaml configuration file.
 *      The service can serialize the data in parallel or in sequence.
 *      The user can define the format how the files are build.
 * 
 */

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using NetFlexor.Interfaces;
using NetFlexor.Data.Transformation;
using NetFlexor.ServiceHelpers;

namespace NetFlexor.Service.FileSerializer
{
    public class FileSerializerService : NetFlexorBaseService
    {
        FileSerializerServiceConfiguration _conf;
        ILogger<FileSerializerService> _logger;
        List<DirectoryHandler> _dirHandlers;
        //bool firstRun = true;
        public FileSerializerService(IFlexorDataBuffer buffer, ILogger<FileSerializerService> logger) : base(buffer)
        {
            _logger = logger;
        }
        public override Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            // TODO: Add configuration check so that the configuration is not null
            _conf = conf as FileSerializerServiceConfiguration;

            // This will create the handlers for the 
            _dirHandlers = new List<DirectoryHandler>();
            foreach (var item in _conf.Linked)
            {
                string path = item.Folder ?? _conf.Folder;
                if (!_dirHandlers.Any(x => x.path == path))
                    _dirHandlers.Add(new DirectoryHandler(path, item.Name, 
                        (item.FileSuffix ?? _conf.FileSuffix), 
                        (item.FilePrefix ?? _conf.FilePrefix)));
                else
                    _dirHandlers.First(x => x.path == path).AddNewServiceName(
                        item.Name, (item.FileSuffix ?? _conf.FileSuffix), 
                        (item.FilePrefix ?? _conf.FilePrefix));
            }
            return Task.CompletedTask;
        }

        public override async Task WorkAsync(CancellationToken stoppingToken)
        {
            long interval = _conf.getExecutionInterval(timeUnit.ms);
            _logger.LogInformation("FileSerializerService service is running.");
            Stopwatch sw = new Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                // always skip the first run since there cannot be new data
                sw.Restart();
                try
                {
                    // TODO: Add check if we want to run these in parallel or in sequence -> _conf.ExecutionFormat
                    List<Task> tasks = new List<Task>();
                    var exFormat = _conf.GetExecutionFormat();
                    foreach (var item in _conf.Linked)
                    {
                        switch (exFormat)
                        {
                            case ExecutionFormat.Parallel:
                                tasks.Add(Task.Run(() => SerializeDataAsync(item)));
                                break;
                            case ExecutionFormat.Sequence:
                                await SerializeDataAsync(item);
                                break;
                            default:
                                break;
                        }
                    }
                    await Task.WhenAll(tasks);

                    //_logger.LogInformation($"Tasks executed: {tasks.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                sw.Stop();
                // Try to keep the execution interval as close as possible
                // If the execution took longer than the interval, skip the delay
                _logger.LogInformation($"Execution time: {sw.ElapsedMilliseconds} ms");
                if (interval > sw.ElapsedMilliseconds)
                    await Task.Delay((int)(interval - sw.ElapsedMilliseconds), stoppingToken);
            }
        }

        /// <summary>
        /// Serialize the data from the buffer to the file
        /// </summary>
        /// <param name="linked"></param>
        /// <returns></returns>
        private async Task SerializeDataAsync(LinkedService linked)
        {
            // Get the directory handler for the current link item
            var dirHandler = _dirHandlers.First(x => x.path == linked.FullDirectoryPath);

            // dequeue all data from the buffer
            string prefix = linked.FilePrefix ?? _conf.FilePrefix;
            string suffix = linked.FileSuffix ?? _conf.FileSuffix;
            string fileContent = "";
            string fileName = $"{prefix}_{DateTime.Now}{suffix}";
            //var data = DequeueAllDataFromBuffer(item.GetName(), stoppingToken);
            var dataformat = linked.DataFormat ?? _conf.DataFormat;
            // remove two last characters from the string since they are always "\n" because of the yaml configuration file
            dataformat = dataformat.Substring(0, dataformat.Length - 1);
            (int start, int start_end) = TransformDataFormat.GetDataInd(dataformat, "$start(", ")", false);
            string fileStartContent = "";
            if (start >= 0 && start_end >= 0)
            {
                var startContent = dataformat.Substring(start, start_end - start);
                dataformat = dataformat.Replace(startContent, "");
                fileStartContent = startContent.Substring(7, startContent.Length - 8);
            }
            if (dataformat is null)
                fileContent += "[\n";
            while (TryDequeueDataFromBuffer(linked.Name, out var data))
            {
                fileContent += TransformDataFormat.DataFormatParser(dataformat, data);
                if (dataformat is null)
                    fileContent += ",\n";
            }
            if (dataformat is null)
            {
                // remove the last three characters from the string since they are always ",\n"
                fileContent = fileContent.Substring(0, fileContent.Length - 2);
                fileContent += "\n]";
            }

            if (fileContent.Length > 0)
                dirHandler.SerializeNewFileAndDeleteOverflowFilesWithSizeLimits(linked.Name, fileStartContent + fileContent,
                    linked.allowedFolderSizeInBytes, linked.AllowedFileCount);
        }
    }
}
