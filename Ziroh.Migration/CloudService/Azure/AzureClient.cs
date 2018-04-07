using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.CloudService.Azure
{
    public class AzureClient
    {
        public static CloudBlobClient GetClient()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(System.Configuration.ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            return client;
        }
    }
}
