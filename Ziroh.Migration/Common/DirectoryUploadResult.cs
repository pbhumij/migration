using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Migration.Resource;

namespace Migration.Common
{
    public class DirectoryUploadResult
    {
        public bool status = false;
        private string toUploadDirectoryPath = null;
        private string destinationDirectoryId = null;
        private ICloudServiceIO cloudService = null;
        private Queue<DirectoryBlock> queue = null;

        public DirectoryUploadResult(ICloudServiceIO cloudService, string toUploadDirectoryPath, string destinationDirectoryId)
        {
            this.cloudService = cloudService;
            this.toUploadDirectoryPath = toUploadDirectoryPath;
            this.destinationDirectoryId = destinationDirectoryId;
        }

        public bool UploadDirectory()
        {
            bool status = false;
            if (System.IO.Directory.Exists(toUploadDirectoryPath))
            {
                int index = toUploadDirectoryPath.LastIndexOf("/");
                string directoryName = toUploadDirectoryPath.Substring(index);
                string newDirectoryId = cloudService.CreateDirectory(destinationDirectoryId, directoryName);

                DirectoryBlock directory = new DirectoryBlock()
                {
                    id = newDirectoryId,
                    path = toUploadDirectoryPath,
                    Name = directoryName,
                };

                queue.Enqueue(directory);
                Traverse();
            }           
            return status;
        }

        public void Traverse()
        {
            if (queue.Count == 0)
                return;
            while(queue.Count > 0)
            {
                DirectoryBlock currentDirectory = queue.Dequeue();
                System.IO.DirectoryInfo currentDirectoryInfo = new DirectoryInfo(currentDirectory.path);
                FileSystemInfo[] files = currentDirectoryInfo.GetFileSystemInfos();
                List<Task> tasks = new List<Task>();
                foreach (var file in files)
                {
                    if (file.GetType() == typeof(System.IO.DirectoryInfo))
                    {
                        string DirectoryId = cloudService.CreateDirectory(destinationDirectoryId, file.Name);
                        DirectoryBlock directory = new DirectoryBlock()
                        {
                            id = DirectoryId,
                            Name = file.Name,
                            path = file.FullName,
                        };
                        queue.Enqueue(directory);
                    }
                    if(file.GetType() == typeof(System.IO.FileInfo))
                    {
                        FileBlock newFile = new FileBlock()
                        {
                            Name = file.Name,
                            Parent = new List<string> { currentDirectory.id}
                        };
                        cloudService.UploadCloudFile(newFile, file.FullName);
                    }
                }
                Task.WaitAll(tasks.ToArray());
                Traverse();
            }

        }
    }
}
