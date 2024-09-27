/*
 * 
 * Author: Sincerelord2
 * 
 * Copying and/or modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This class contains tools and helpers that are used by the BufferHandlerService.
 * 
 */

using NetFlexor.Interfaces;
using System.Text.Json;
using NetFlexor.ServiceHelpers;

namespace NetFlexor.Service.BufferHandler
{
    internal static class BufferHandlerToolKit
    {
        public static bool TryGetQueueByStringKey(Dictionary<(string, INetFlexorService), (long size, Queue<FlexorDataBufferContainer> q)> dictionary,
            string searchString, out (long size, Queue<FlexorDataBufferContainer> q)? queue)
        {
            // Find the key that matches the searchString
            var key = dictionary.Keys.FirstOrDefault(k => k.Item1 == searchString);
            queue = null;

            // If no key was found, return null
            if (key == default)
            {
                return false;
            }

            //// Use the key to retrieve the value from the dictionary
            //if (dictionary.TryGetValue(key, out var newQueue))
            //{
            //    queue = newQueue;
            //    return true;
            //}
            return false;
        }

        public static void StreamContainerToFile(FlexorDataBufferContainer dataContainer, DirectoryHandler dirHandler, DirectoryHandlerObject dirObj)
        {
            // Serialize the dataContainer to a JSON string
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(dataContainer, new JsonSerializerOptions { WriteIndented = false });

            // Append the serialized dataContainer to the file
            using (var fileStream = new FileStream($"{dirHandler.path}{Path.DirectorySeparatorChar}{dirObj.FilePrefix}{dirObj.FileSuffix}", FileMode.Append))
            using (var writer = new StreamWriter(fileStream))
            {
                writer.WriteLine(jsonContent);
            }
        }
        public static void WriteBufferDataToDisk(FlexorDataBufferContainer dataContainer, DirectoryHandler dirHandler, DirectoryHandlerObject dirObj)
        {
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(dataContainer/*.GetSaveModel()*/, new JsonSerializerOptions { WriteIndented = true });

            string fullFileName = $"{dirHandler.path}{Path.DirectorySeparatorChar}{dirObj.FilePrefix}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{dirObj.FileSuffix}";
            // Write the serialized dataContainer to the file with unix timestamp as the file name
            // Using System.IO.File.WriteAllText to write the file
            if (!File.Exists(fullFileName))
                System.IO.File.WriteAllText(fullFileName, jsonContent);
            else
            {
                // handle the case where the file already exists
                // This should not happen and i don't know what to do here yet
            }
        }
        public static List<FlexorDataBufferContainer> ReadBufferDataFromDisk(DirectoryHandler dirHandler, DirectoryHandlerObject dirObj, long allowedBufferSize, long currentBufferSize)
        {
            // List all the files in the directory for this service disk buffer
            var files = dirObj.getFiles(dirHandler.dirInfo);

            // Create a list to store the SyncDataBufferContainers
            var containers = new List<FlexorDataBufferContainer>();

            // get at least the last 5 files in the list based on the unix timestamp in the file name
            int getFileCount = 5;

            if (files.Count < 5)
                getFileCount = files.Count;

            //var temp = files.OrderByDescending(f => long.Parse(f.Name.Split('_').Last().Split('.')[0]));

            var lastFiles = files.OrderByDescending(f => long.Parse(f.Name.Split('_').Last().Split('.')[0])).Take(getFileCount);

            foreach (var item in lastFiles)
            {
                // Read the content of the file
                string jsonContent = System.IO.File.ReadAllText(item.FullName);

                // Deserialize the content of the file into a SyncDataBufferContainer
                //var container = System.Text.Json.JsonSerializer.Deserialize<SyncDataBufferContainerSaveModel>(jsonContent);
                var container = System.Text.Json.JsonSerializer.Deserialize<FlexorDataBufferContainer>(jsonContent);

                // Calculate the size of the container
                var containerSize = ObjectSizeCalculator.GetObjectSize(container);

                // Check if the container can be added to the list
                if (containerSize + currentBufferSize <= allowedBufferSize)
                {
                    var newContainer = new FlexorDataBufferContainer();
                    //newContainer.UploadSaveModel(container);
                    //containers.Add(newContainer);
                    containers.Add(container);
                    currentBufferSize += containerSize;
                    File.Delete(item.FullName); // delete old file
                }
                else // If the container is too large, stop reading the files
                    break;
            }
            // Return the list of SyncDataBufferContainers
            return containers;
        }
        public static bool PossiblyDeleteOldBufferData(DirectoryHandler dirHandler, DirectoryHandlerObject dirObj, long appendSize, long allowedSize, bool BufferEachContainer)
        {
            // Calculate the size of the directory
            dirObj.CalulateFolderSize(dirHandler.dirInfo);

            if (dirObj.size + appendSize >= allowedSize)
            {
                if (BufferEachContainer)
                {
                    // Each data container is written to a separate file so just handle with the dirObj
                    return dirObj.CheckDirectorySizeAndFileCount(dirHandler.dirInfo, allowedSize, 0);
                }
                else
                {
                    // NOT IN USE since currently
                    // The writing part has not been implemented so that is why this is not in use

                    // Each data container is written to the same file
                    // Delete the oldest line from the file (Should be first line in the file)
                    // using FileStream to delete first line from the file
                    using (var fileStream = new FileStream(
                        $"{dirHandler.path}{Path.DirectorySeparatorChar}{dirObj.FilePrefix}{dirObj.FileSuffix}", 
                        FileMode.Open, FileAccess.ReadWrite))
                    {
                        // Create a new StreamReader to read the file
                        using (var reader = new StreamReader(fileStream))
                        {
                            // Read the first line and ignore it
                            reader.ReadLine();
                        }

                        // Create a new StreamWriter to write the remaining lines back to the file
                        using (var writer = new StreamWriter(fileStream))
                        {
                            // Set the file position to the beginning
                            fileStream.Position = 0;

                            // Create a new StreamReader to read the remaining lines
                            using (var remainingReader = new StreamReader(fileStream))
                            {
                                // Read the remaining lines and write them back to the file
                                writer.Write(remainingReader.ReadToEnd());
                            }
                        }
                    }
                }
            }
            return false;

            // Check if the directory is over the size limit
            //return dirObj.IsOverSizeLimit(1000000);
        }
        /// <summary>
        /// Not fully implemented and tested, so do not use in production version
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static List<FlexorDataBufferContainer> ReadAndClearContainersFromFile(string filePath)
        {
            var containers = new List<FlexorDataBufferContainer>();
            var linesToKeep = new List<string>();

            // Use FileStream to read the file
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(fileStream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        // Attempt to deserialize each line into a SyncDataBufferContainer
                        var container = JsonSerializer.Deserialize<FlexorDataBufferContainer>(line);
                        if (container != null)
                        {
                            containers.Add(container);
                        }
                        else
                        {
                            // If deserialization fails, keep the line for later
                            linesToKeep.Add(line);
                        }
                    }
                    catch (JsonException)
                    {
                        // If deserialization fails, keep the line for later
                        linesToKeep.Add(line);
                    }
                }
            }

            // Decide whether to update the file or delete it
            if (linesToKeep.Count > 0)
            {
                // If there are lines left, write them back to the file using FileStream
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(fileStream))
                {
                    foreach (var remainingLine in linesToKeep)
                    {
                        writer.WriteLine(remainingLine);
                    }
                }
            }
            else
            {
                // If no lines are left, delete the file
                File.Delete(filePath);
            }

            return containers;
        }
    }
}
