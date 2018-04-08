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
    public class CloudMigration : MigrationResource
    {
        private ICloudServiceIO cloudService = null;
        private Queue<DirectoryBlock> queue = null;
        private bool DownloadCompleteStatus = true;

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
                DownloadCloudDirectory downloadDirectory = new DownloadCloudDirectory(toDownloadBlock, "C:\\CloudFiles\\", cloudService);
                downloadDirectory.DownloadAsync();
                CleanUpTransactionFile();
                if (DownloadCompleteStatus == true)
                    System.IO.File.Delete(TransactionFilePath);
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
                        if (directory.DownloadStatus == false)
                        {
                            DownloadCompleteStatus = false;
                            break;
                        }
                        else                       
                            queue.Enqueue(directory);
                    }                 
                }
                if(currentDirectory.Files != null)
                {
                    foreach(var file in currentDirectory.Files)
                    {
                        if(file.DownloadStatus == false)
                        {
                            DownloadCompleteStatus = false;
                            break;
                        }
                    }
                }
            }
            Traverse();
        }
    }
}
