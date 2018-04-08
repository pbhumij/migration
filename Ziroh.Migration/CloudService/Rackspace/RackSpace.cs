using Migration.Common;
using Migration.Resource;
using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackSpaceClassLibrary
{
    public class RackSpace : ICloudServiceIO
    {
        private readonly CloudFilesProvider client = Client.GetClient();
        int limit = 100;
        bool list_continue = false;

        public string sourceFolder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string GetRootId()
        {
            return "root";
        }

        public void GetFiles(DirectoryBlock directory)
        {
            if (directory.id == "root")
            {
                var items = client.ListContainers(limit: limit);
                do
                {
                    string marker = null;
                    List<Task> tasks = new List<Task>();
                    foreach (var item in items)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            AddToParentDirectoryObject(item, directory);
                            marker = item.Name;
                        }));
                    }
                    Task.WaitAll(tasks.ToArray());
                    if (items.Count() == limit)
                    {
                        list_continue = true;
                        items = client.ListContainers(limit: limit, marker: marker);
                    }
                    else
                        list_continue = false;
                }
                while (list_continue);
            }
            else
            {
                var items = client.ListObjects(directory.id, prefix: directory.prefix, limit: limit);
                do
                {
                    string marker = null;
                    if (items != null)
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (var item in items)
                        {
                            tasks.Add(Task.Run(() =>
                            {
                                var header = client.GetObjectHeaders(directory.id, item.Name);
                                foreach (var keypair in header)
                                {
                                    Console.WriteLine(item.Name + " " + keypair.Key + " " + keypair.Value);
                                }
                                AddToParentDirectoryObject(item, directory, directory.prefix);
                                marker = item.Name;
                            }));
                        }
                        Task.WaitAll(tasks.ToArray());
                    }
                    if (items.Count() == limit)
                    {
                        list_continue = true;
                        items = client.ListObjects(directory.id, prefix: directory.prefix, limit: limit, marker: marker);
                    }
                    else
                        list_continue = false;
                } while (list_continue);
            }
        }


        internal void AddToParentDirectoryObject(Container item, DirectoryBlock directory)
        {
           if(directory.SubDirectories == null)
            {
                directory.SubDirectories = new List<DirectoryBlock>();
            }
            directory.SubDirectories.Add(CreateDirectoryObject(item));
        }

        /// <summary>
        /// Identifies objects which are immediate child of the parent directory and adds them to the parent directory object
        /// </summary>
        /// <param name="item"></param>
        /// <param name="directory"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        internal void AddToParentDirectoryObject(ContainerObject item, DirectoryBlock directory, string prefix)
        {
            string tempName = RemovePrefixFromName(item, prefix);
            var tempList = new List<ContainerObject>();
            // if after removing the prefix from file name, the file name still contains a '/' it means this object is not directly related to parent object.
            // we add only the directly related objects to the templist
            if (!tempName.Contains('/'))
                tempList.Add(item);
            if(tempList != null)
            {
                foreach (var obj in tempList)
                {
                    if (obj.ContentType == "application/directory")
                    {
                        if (directory.SubDirectories == null)
                            directory.SubDirectories = new List<DirectoryBlock>();
                        directory.SubDirectories.Add(CreateDirectoryObject(obj, directory, prefix));
                    }
                    else
                    {
                        if (directory.Files == null)
                            directory.Files = new List<FileBlock>();
                        directory.Files.Add(CreateFileObject(obj, directory, prefix));
                    }
                }
            }          
        }

        internal DirectoryBlock CreateDirectoryObject(Container item)
        {
            DirectoryBlock newDirectory = new DirectoryBlock()
            {
                id = item.Name,
                Name = item.Name,
                prefix = "",
                SharedLink = GetSharedLink(item),
                path = item.Name,
            };
            return newDirectory;
        }

        internal string RemovePrefixFromName(ContainerObject item, string prefix)
        {
            String temp = null;
            if (item.Name.StartsWith(prefix))
            {
                temp = item.Name.Substring(prefix.Length);
            }
            return temp;
        }

        internal DirectoryBlock CreateDirectoryObject(ContainerObject item, DirectoryBlock directory, string prefix)
        {
            DirectoryBlock newDirectory = new DirectoryBlock()
            {
                Name = RemovePrefixFromName(item, prefix),
                prefix = item.Name+"/",
                id = directory.id,
                path = directory.path+"\\" + RemovePrefixFromName(item, prefix),
                container = directory.id,
            };
            return newDirectory;
        }

        internal FileBlock CreateFileObject(ContainerObject item, DirectoryBlock directory, string prefix)
        {
            FileBlock newFile = new FileBlock()
            {
                id = RemovePrefixFromName(item, prefix),
                Name = RemovePrefixFromName(item, prefix),
                Size = item.Bytes,
                SharedLink = GetSharedLink(directory, item, prefix),
                path = directory.path+"\\"+ RemovePrefixFromName(item, prefix),
                container = directory.id,
            };
            return newFile;
        }

        internal string GetSharedLink(Container item)
        {
            ContainerCDN container = null;
            try
            {
                container = client.GetContainerCDNHeader(container: item.Name);
            }
            catch (Exception e)
            {
                throw e;
            }
            return container.CDNUri;
        }
        internal string GetSharedLink(DirectoryBlock directory, ContainerObject item, string prefix)
        {
            string url = null;
            try
            {
                ContainerCDN CDN = client.GetContainerCDNHeader(container: directory.id);
                url = CDN.CDNUri +"/"+ RemovePrefixFromName(item, prefix);
            }
            catch (Exception e)
            {
                throw e;
            }
            return url;
        }

        public void CreateSharedLink()
        {
            client.EnableCDNOnContainer("ostorCloudTest1", false);
        }

        public bool UploadCloudFile(FileBlock file, string destinationPath = null)
        {
            bool status = false;
            try
            {
                Task task = Task.Run(() => client.CreateObjectFromFile(file.container, file.path, objectName: file.id));
                task.Wait();
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
            using (var outStream = new FileStream(localPath+file.path, FileMode.OpenOrCreate))
            {
                client.GetObject(file.container, file.id, outStream);
            }
        }

        public long GetTotalSpace()
        {
            throw new NotImplementedException();
        }

        public long GetUsedSpace()
        {
            throw new NotImplementedException();
        }
    }
}
