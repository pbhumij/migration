using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ziroh.Migration.MigrationIO;

namespace Migration.Common
{
    public class DownloadCloudDirectory : MigrationResource
    {
        public bool status = false;
        private string toDownloadDirectoryPath = null;
        private DirectoryBlock directory = null;
        private Queue<DirectoryBlock> queue = null;
        private ICloudServiceIO cloudService;

        public DownloadCloudDirectory(DirectoryBlock directory, string toDownloadDirectoryPath, ICloudServiceIO cloudService)
        {
            this.directory = directory;
            this.toDownloadDirectoryPath = toDownloadDirectoryPath;
            this.cloudService = cloudService;
        }

        public bool DownloadAsync()
        {
            bool status = false;
            queue = new Queue<DirectoryBlock>();
            queue.Enqueue(directory);
            Traverse().GetAwaiter().GetResult();
            return status;
        }

        private async Task Traverse()
        {
            if (queue.Count == 0)
                return;
            while (queue.Count > 0)
            {
                var currentDirectory = queue.Dequeue();
                if (currentDirectory.SubDirectories != null)
                {
                    DownloadDirectory(currentDirectory.SubDirectories);
                }
                if (currentDirectory.Files != null)
                {
                    DownloadFiles(currentDirectory.Files);
                }
            }
            await Traverse();
        }

        private void DownloadFiles(List<FileBlock> files)
        {
            List<Task> downloadFilesTask = new List<Task>();
            foreach (var file in files)
            {
                if(file.DownloadStatus == false)
                {
                    downloadFilesTask.Add(Task.Run(() =>
                    {
                        cloudService.DownloadCloudFile(file, toDownloadDirectoryPath);
                        file.DownloadStatus = true;
                        TransactionFile transactionFile = new TransactionFile();
                        transactionFile.Create(directory);
                    }));
                }
            }
            Task.WaitAll(downloadFilesTask.ToArray());
        }

        private void DownloadDirectory(List<DirectoryBlock> subDirectories)
        {
            List<Task> downloadDirectoriesTask = new List<Task>();
            foreach (var subDirectory in subDirectories)
            {
                if(subDirectory.DownloadStatus == false)
                {
                    downloadDirectoriesTask.Add(Task.Run(() =>
                    {
                        System.IO.Directory.CreateDirectory(toDownloadDirectoryPath + subDirectory.path);
                        subDirectory.DownloadStatus = true;
                        queue.Enqueue(subDirectory);
                        TransactionFile transactionFile = new TransactionFile();
                        transactionFile.Create(directory);
                    }));
                }
            }
            Task.WaitAll(downloadDirectoriesTask.ToArray());
        }
    }
}
