/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Directory handler object class is used to handle directories and their contents (size, file count and path).
 * 
 */

namespace NetFlexor.ServiceHelpers
{
    public class DirectoryHandlerObject
    {
        public string FilePrefix { get; set; }
        public string FileSuffix { get; set; }
        /// <summary>
        /// Folder size in bytes.
        /// </summary>
        public long size { get; set; }
        /// <summary>
        /// Number of files in the folder.
        /// </summary>
        public long fileCount { get; set; }

        public DirectoryHandlerObject(string filePrefix, string fileSuffix)
        {
            FilePrefix = filePrefix;
            FileSuffix = fileSuffix;
        }
        public List<FileInfo> getFiles(DirectoryInfo dir)
        {
            return dir.GetFiles("*" + FileSuffix)
            .Where(file => file.Name.StartsWith(FilePrefix))
            .ToList();
        }
        public void CalulateFolderSize(DirectoryInfo dir)
        {
            size = 0;
            fileCount = 0;

            foreach (var file in getFiles(dir))
            {
                size += file.Length;
                fileCount++;
            }
        }
        private bool IsOverSizeLimit(long limit)
        {
            return size > limit;
        }
        private bool IsOverFileCountLimit(long limit)
        {
            return fileCount > limit;
        }
        private void deleteOldestFile(DirectoryInfo dir)
        {
            var files = getFiles(dir);
            if (files.Count > 0)
            {
                var oldestFile = files.OrderBy(f => f.LastWriteTime).First();
                oldestFile.Delete();
            }
        }
        public bool CheckDirectorySizeAndFileCount(DirectoryInfo dir,
            long sizeLimit, long fileCountLimit)
        {
            bool deleted = false;
            while (sizeLimit > 0 && IsOverSizeLimit(sizeLimit))
            {
                // Delete
                deleteOldestFile(dir);
                fileCount--;
                // Recalculate
                CalulateFolderSize(dir);
                deleted = true;
            }

            while (fileCountLimit > 0 && IsOverFileCountLimit(fileCountLimit))
            {
                // Delete
                deleteOldestFile(dir);
                fileCount--;
                // Recalculate
                CalulateFolderSize(dir);
                deleted = true;
            }

            return deleted;
        }
    }
}
