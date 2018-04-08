using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Migration.Common;
using Migration.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.CloudService.Azure
{
    public class AzureService : ICloudServiceIO
    {
        CloudBlobClient client = null;
        private int maxListResults = 100;


        public AzureService()
        {
            client = AzureClient.GetClient();
        }

        public string GetRootId()
        {
            return "root";
        }

        public void GetFiles(DirectoryBlock parentDirectory)
        {
            if (parentDirectory.id == "root")
            {
                Task task = Task.Run(() => AddContainersToRoot(parentDirectory));
                task.Wait();
            }
            else if (parentDirectory.type == "container")
            {
                Task task = Task.Run(() => AddBlobsToContainer(parentDirectory));
                task.Wait();
            }
            else if (parentDirectory.type == "BlobDirectory")
            {
                Task task = Task.Run(() => AddBlobsToDirectory(parentDirectory));
                task.Wait();
            }
        }

        internal async Task AddContainersToRoot(DirectoryBlock directory)
        {
            var items = await client.ListContainersSegmentedAsync(null, new ContainerListingDetails(),
                              maxListResults,
                              new BlobContinuationToken(),
                              new BlobRequestOptions(),
                              new OperationContext());
            var list_continue = false;
            do
            {
                if (items != null)
                {
                    foreach (var item in items.Results)
                    {
                        AddToParentDirectoryObject(directory, item);
                    }
                }
                if (items.ContinuationToken != null)
                {
                    items = await client.ListContainersSegmentedAsync(null, new ContainerListingDetails(), maxListResults, items.ContinuationToken, new BlobRequestOptions(), new OperationContext());
                    list_continue = true;
                }
                else
                    list_continue = false;
            } while (list_continue);
        }

        internal async Task AddBlobsToContainer(DirectoryBlock directory)
        {
            var container = client.GetContainerReference(directory.Name);
            var items = await container.ListBlobsSegmentedAsync(null, false, new BlobListingDetails(), maxListResults, new BlobContinuationToken(), null, null);
            var list_continue = false;
            do
            {
                foreach (var item in items.Results)
                {
                    AddToParentDirectoryObject(directory, item);
                }
                if (items.ContinuationToken != null)
                {
                    items = await client.ListBlobsSegmentedAsync(null, false, new BlobListingDetails(), maxListResults, items.ContinuationToken, null, null);
                    list_continue = true;
                }
                else
                    list_continue = false;
            } while (list_continue);
        }

        internal async Task AddBlobsToDirectory(DirectoryBlock directory)
        {
            var container = client.GetContainerReference(directory.container);
            CloudBlobDirectory blobDirectory = container.GetDirectoryReference(directory.prefix);
            var items = await blobDirectory.ListBlobsSegmentedAsync(false, new BlobListingDetails(),
                              maxListResults,
                              new BlobContinuationToken(),
                              new BlobRequestOptions(),
                              new OperationContext());
            var list_continue = false;
            do
            {
                foreach (IListBlobItem item in items.Results)
                {
                    AddToParentDirectoryObject(directory, item);
                }
                if (items.ContinuationToken != null)
                {
                    items = await blobDirectory.ListBlobsSegmentedAsync(false, new BlobListingDetails(),
                                  maxListResults,
                                  items.ContinuationToken,
                                  new BlobRequestOptions(),
                                  new OperationContext());
                    list_continue = true;
                }
                else
                    list_continue = false;
            } while (list_continue);
        }

        internal void AddToParentDirectoryObject(DirectoryBlock directory, CloudBlobContainer item)
        {
            if (directory.SubDirectories == null)
                directory.SubDirectories = new List<DirectoryBlock>();
            DirectoryBlock newContainer = new DirectoryBlock()
            {
                Name = item.Name,
                type = "container",
                path = item.Name,
                accessType = GetAccessType((CloudBlobContainer)item),
            };
            directory.SubDirectories.Add(newContainer);
        }

        internal void AddToParentDirectoryObject(DirectoryBlock directory, IListBlobItem item)
        {
            if (item.GetType() == typeof(CloudBlobDirectory))
            {
                if (directory.SubDirectories == null)
                    directory.SubDirectories = new List<DirectoryBlock>();
                directory.SubDirectories.Add(CreateDirectoryObject(item, directory));
            }
            if (item.GetType() == typeof(CloudBlockBlob))
            {
                var file = (CloudBlob)item;
                if (directory.Files == null)
                    directory.Files = new List<FileBlock>();
                directory.Files.Add(CreateFileObject(item, directory));
            }
        }

        internal DirectoryBlock CreateDirectoryObject(IListBlobItem item, DirectoryBlock directory)
        {
            DirectoryBlock newDirectory = new DirectoryBlock()
            {
                Name = GetName(item),
                container = item.Container.Name,
                type = "BlobDirectory",
                prefix = directory.prefix + GetName(item) + "/",
                path = directory.path + "\\" + GetName(item),
                CloudUri = item.Uri,
            };
            return newDirectory;
        }

        internal FileBlock CreateFileObject(IListBlobItem item, DirectoryBlock directory)
        {
            FileBlock file = new FileBlock()
            {
                Name = GetName(item),
                //Size = blob.Properties.Length,
                container = item.Container.Name,
                SharedLink = item.Uri.ToString(),
                path = directory.path + "\\" + GetName(item),
                CloudUri = item.Uri,
            };
            return file;
        }

        internal string GetName(IListBlobItem item)
        {
            string name = item.Uri.ToString();
            string parent = item.Parent.Uri.ToString();
            if (parent.EndsWith("/"))
                name = name.Substring(parent.Length);
            else
                name = name.Substring((parent.Length) + 1);
            if (!(item.GetType() == typeof(CloudBlob)))
            {
                if (name.EndsWith("/"))
                    name = name.Substring(0, name.Length - 1);
                else
                    name = name.Substring(0, name.Length);
            }
            return name;
        }

        internal string GetPath(IListBlobItem item)
        {
            Console.WriteLine(item.Uri);
            return null;
        }

        internal string GetAccessType(CloudBlobContainer item)
        {
            if (item.Properties.PublicAccess.ToString().Equals("Blob"))
                return item.Properties.PublicAccess + " (anonymous read access for blobs only)";
            if (item.Properties.PublicAccess.ToString().Equals("Container"))
                return item.Properties.PublicAccess + " (anonymous read access for containers and blobs)";
            else
                return "private";
        }

        public void UploadDirectory(DirectoryBlock item)
        {
            throw new NotImplementedException();
        }

        public long GetTotalSpace()
        {
            throw new NotImplementedException();
        }

        public long GetUsedSpace()
        {
            throw new NotImplementedException();
        }

        public bool UploadCloudFile(FileBlock file, string destintationPath = null)
        {
            bool status = false;
            try
            {
                Task<CloudBlobContainer> containerReference = Task.Run(() => client.GetContainerReference(file.container));
                var container = containerReference.Result;
                CloudBlockBlob blob = container.GetBlockBlobReference(file.id);
                Task uploadtask = Task.Run(() => blob.UploadFromFileAsync(file.path));
                uploadtask.Wait();
                status = true;
            }
            catch (Exception e)
            {
                throw e;
            }
            return status;
        }

        public bool CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            throw new NotImplementedException();
        }

        string ICloudServiceIO.CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            throw new NotImplementedException();
        }

        public void DownloadCloudFile(FileBlock file, string localPath)
        {
            try
            {
                Uri blobUri = file.CloudUri;
                Task<ICloudBlob> task = Task.Run(() => client.GetBlobReferenceFromServerAsync(blobUri));
                var response = task.Result;
                using (var outStream = new FileStream(localPath + file.path, FileMode.OpenOrCreate))
                {
                    response.DownloadToStream(outStream);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

    }
}
