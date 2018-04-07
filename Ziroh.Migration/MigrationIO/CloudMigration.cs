using Migration.Common;
using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ziroh.Migration.MigrationIO;

namespace Migration.Common
{
    public class CloudMigration
    {
        private string TransactionFilePath = null;
        private ICloudServiceIO cloudService = null;
        private Queue<DirectoryBlock> queue = null;

        public CloudMigration(ICloudServiceIO cloudService)
        {
            this.cloudService = cloudService;
        }

        public MigrationResult MigrateToOstor(DirectoryBlock directoryBlock)
        {
            MigrationResult result = new MigrationResult();
            while (System.IO.File.Exists(TransactionFilePath))
            {
                TransactionFile logFile = new TransactionFile();
                DirectoryBlock toDownloadBlock = logFile.Deserialize();
                DownloadCloudDirectory downloadDirectory = new DownloadCloudDirectory(directoryBlock, "C:\\CloudFiles\\", cloudService);
                downloadDirectory.DownloadAsync();
                CleanUpTransactionFile();
            }           
            return result;
        }

        private void CleanUpTransactionFile()
        {
            TransactionFile transactionFile = new TransactionFile();
            DirectoryBlock directoryBlock = transactionFile.Deserialize();
            queue = new Queue<DirectoryBlock>();
            queue.Enqueue(directoryBlock);
            Traverse();
        }

        private void Traverse()
        {
            if (queue.Count == 0)
                return;
            while(queue.Count > 0)
            {
                var currentDirectory = queue.Dequeue();
                if(currentDirectory.SubDirectories != null)
                {
                    foreach(var directory in currentDirectory.SubDirectories)
                    {
                        if (directory.DownloadStatus)
                            currentDirectory.SubDirectories.Remove(directory);
                        else
                            queue.Enqueue(directory);
                    }
                    foreach (var file in currentDirectory.Files)
                    {
                        if (file.DownloadStatus)
                            currentDirectory.Files.Remove(file);
                    }
                }
            }
            Traverse();
        }
    }
}
