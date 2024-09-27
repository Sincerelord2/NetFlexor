/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Directory handler class is used to handle directories and their contents (size, file count and path).
 * 
 */

using System.Text;

namespace NetFlexor.ServiceHelpers
{
    
    /// <summary>
    /// This class is used to handle directories and their contents (size, file count and path).
    /// </summary>
    public class DirectoryHandler
    {
        /// <summary>
        /// Directory info object.
        /// </summary>
        public DirectoryInfo dirInfo;
        /// <summary>
        /// Host service names and their respective directory objects.
        /// </summary>
        private Dictionary<string, DirectoryHandlerObject> serviceNameDict { get; set; } = new();

        /// <summary>
        /// Folder path.
        /// </summary>
        public string path { get; }

        public DirectoryHandler(string path, string serviceName, string fileSuffix, string filePrefix)
        {
            //serviceNames.Add(serviceName);
            AddNewServiceName(serviceName, fileSuffix, filePrefix);
            dirInfo = new DirectoryInfo(path);
            this.path = dirInfo.FullName;
            Initialize();
        }

        public Dictionary<string, DirectoryHandlerObject> GetServiceDict()
        {
            return serviceNameDict;
        }

        public void AddNewServiceName(string serviceName, string fileSuffix, string filePrefix)
        {
            serviceNameDict.Add(serviceName, new DirectoryHandlerObject(filePrefix, fileSuffix));
        }

        /// <summary>
        /// Initialize the directory path. <br></br>
        /// Creates a path if it does not exist.
        /// </summary>
        private void Initialize()
        {
            if (!dirInfo.Exists)
            {
                // split the path into parts
                string[] parts = dirInfo.FullName.Split(Path.DirectorySeparatorChar);

                // for each part, check if it exists and create it if it does not
                for (int i = 0; i < parts.Length; i++)
                {
                    string currentPath = parts[0];
                    for (int j = 1; j <= i; j++)
                    {
                        currentPath += Path.DirectorySeparatorChar + parts[j];
                    }
                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                }
            }
            CalulateDirObjectsSize();
        }

        private void CalulateDirObjectsSize()
        {
            foreach (var item in serviceNameDict)
            {
                item.Value.CalulateFolderSize(dirInfo);
            }
        }

        public void CheckDirectorySizeAndFileCount(long sizeLimit, int fileCountLimit)
        {
            foreach (var item in serviceNameDict)
            {
                item.Value.CheckDirectorySizeAndFileCount(dirInfo, sizeLimit, fileCountLimit);
            }
        }

        private long preCalulateFileSize(string fileContent)
        {
            // Convert the string content to bytes using the specified encoding
            byte[] contentBytes = Encoding.UTF8.GetBytes(fileContent);

            // The length of the byte array represents the size of the content in bytes
            long sizeInBytes = contentBytes.Length;

            return sizeInBytes;
        }

        public void SerializeNewFileAndDeleteOverflowFilesWithSizeLimits(string serviceName, string fileContent, long sizeLimit, long fileCountLimit)
        {
            if (serviceNameDict.ContainsKey(serviceName))
            {
                // get the directory object
                var dirObj = serviceNameDict[serviceName];

                // calculate the size of the new file
                dirObj.CalulateFolderSize(dirInfo);
                long fSize = preCalulateFileSize(fileContent);
                dirObj.size += fSize;
                dirObj.fileCount++;

                // check if the size limit is exceeded with the new file before writing it to the disk
                dirObj.CheckDirectorySizeAndFileCount(dirInfo, sizeLimit, fileCountLimit);

                // write the new file to the disk
                string fileName = $"{dirObj.FilePrefix}_{DateTime.Now}{dirObj.FileSuffix}";
                if (!File.Exists($"{path}{Path.PathSeparator}{fileName}"))
                    File.WriteAllText(Path.Combine(path, fileName), fileContent);
            }
        }
    }
}
