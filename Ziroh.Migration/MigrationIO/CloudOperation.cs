using Microsoft.Win32;
using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Migration.Common
{

    /// <summary>
    /// This class provides common functions to fetch the directory structure+
    /// of different cloud services.
    /// </summary>
    public class CloudOperation : ICloudOperationIO
    {
        ICloudServiceIO cloudService = null;
        Queue<DirectoryBlock> queue = null;
        DirectoryBlock rootDirectory = null;

        public CloudOperation(ICloudServiceIO cloudService)
        {
            this.cloudService = cloudService;
        }

        /// <summary>
        /// Method to fetch the directories and files at the root level.
        /// </summary>
        /// <returns> Root Directory </returns>
        public CloudStructure GetCloudRootStructure()
        {
            string rootId = cloudService.GetRootId();
            CloudStructure rootStructure = new CloudStructure();
            try
            {
                rootDirectory = new DirectoryBlock()
                {
                    id = rootId,
                    Name = "",
                };
                queue = new Queue<DirectoryBlock>();
                queue.Enqueue(rootDirectory);
                Task task = Task.Run(() => Traverse());
                task.Wait();
                rootStructure.RootDirectory = rootDirectory;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " " + e.Source + " " + e.StackTrace);
            }
            return rootStructure;
        }

        /// <summary>
        /// A Breath First Traversal method to traverse through the directories and files in a cloud.
        /// </summary>
        private async Task Traverse()
        {
            try
            {
                if (queue.Count == 0)
                    return;
                DirectoryBlock currentDirectory = null;
                while (queue.Count > 0)
                {
                    currentDirectory = queue.Dequeue();
                    Task task = Task.Run(() => cloudService.GetFiles(currentDirectory));
                    task.Wait();
                    if (currentDirectory.SubDirectories != null)
                    {
                        List<Task> enqueueTask = new List<Task>();
                        foreach (var subDirectory in currentDirectory.SubDirectories)
                        {
                            if(subDirectory != null)
                            enqueueTask.Add(Task.Run(() => queue.Enqueue(subDirectory)));
                        }
                        Task.WaitAll(enqueueTask.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            await Traverse();
        }

      

        public FileDownloadResult DownloadCloudFile(string cloudfileId, string localPath, FileBlock file = null)
        {
            try
            {
                FileDownloadResult result = new FileDownloadResult();
                FileBlock newFile = new FileBlock()
                {
                    id = cloudfileId,
                };
                Task task = Task.Run(() => cloudService.DownloadCloudFile(newFile, localPath));
                task.Wait();
                result.status = true;
                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public FileUploadResult UploadCloudFile(string filePath, string destinationDirectoryId)
        {
            FileUploadResult result = new FileUploadResult();
            FileInfo info = new FileInfo(filePath);

            FileBlock file = new FileBlock()
            {
                id = "",
                Parent = new List<string> { destinationDirectoryId },
                path = filePath,
                mimeType = GetMimeType(info),
            };
            cloudService.UploadCloudFile(file);
            result.status = true;
            return result;
        }

        private string GetMimeType(FileInfo info)
        {
            RegistryKey classes = Registry.ClassesRoot;
            // find the sub key based on the file extension
            RegistryKey fileClass = classes.OpenSubKey(Path.GetExtension(info.Extension));
            string contentType = fileClass.GetValue("Content Type").ToString();
            return contentType;
        }

        public StorageQuota GetStorageQuota()
        {
            StorageQuota quota = new StorageQuota();
            quota.TotalSpace = cloudService.GetTotalSpace();
            quota.UsedSpace = cloudService.GetUsedSpace();
            return quota;
        }

        public string GetRootId()
        {
            var rootId = cloudService.GetRootId();
            return rootId;
        }

        public DirectoryUploadResult UploadCloudDirectory(string localDirectoryPath, string destinationDirectoryId)
        {
            DirectoryUploadResult result = new DirectoryUploadResult(cloudService, localDirectoryPath, destinationDirectoryId);
            var response = result.UploadDirectory();
            result.status = true;
            return result;
        }

        public CloudCreateDirectoryResult CreateDirectory(string directoryName = null, string destinationDirectoryId = null, string relativePath = null)
        {
            throw new NotImplementedException();
        }

        public MigrationResult MigrateToOstor(DirectoryBlock directoryBlock, ICloudServiceIO cloudService)
        {
            MigrationResult result = new MigrationResult();
            DownloadCloudDirectory downloadDirectory = new DownloadCloudDirectory(directoryBlock, "C:\\CloudFiles\\", cloudService);
            downloadDirectory.DownloadAsync();
            return result;
        }

        public DownloadCloudDirectory DownloadCloudDirectory(string cloudDirectoryId, string localPath)
        {
            throw new NotImplementedException();
        }

      
    }
}