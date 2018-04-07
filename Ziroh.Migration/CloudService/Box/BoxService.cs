using Box.V2.Exceptions;
using Box.V2.Models;
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
    public class BoxService : ICloudServiceIO
    {
        private readonly Box.V2.BoxClient client = null;
        public string sourceFolder { get; set; }

        public BoxService(string rootPath = null)
        {
            this.sourceFolder = rootPath;
            client = BoxClient.GetClient();
        }

        string ICloudServiceIO.GetRootId()
        {
            return "0";
        }

        public void GetFiles(DirectoryBlock parentDirectory)
        {
            try
            {
                Task<BoxCollection<BoxItem>> task = Task.Run(() => client.FoldersManager.GetFolderItemsAsync(parentDirectory.id, 500, fields: new string[]
                { "id", "name", "shared_link", "description", "size", "owned_by", "createdAt" }));
                BoxCollection<BoxItem> items = task.Result;
                if (items != null)
                {
                    foreach (Box.V2.Models.BoxItem item in items.Entries)
                    {
                        AddToParentDirectoryObject(item, parentDirectory);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " " + e.StackTrace);
            }
        }

        internal void AddToParentDirectoryObject(Box.V2.Models.BoxItem item, DirectoryBlock directory)
        {
            if (isDirectory(item.Type))
            {
                DirectoryBlock newDirectory = CreateDirectoryObject(item, directory);
                if (directory.SubDirectories == null)
                    directory.SubDirectories = new List<DirectoryBlock>();
                directory.SubDirectories.Add(newDirectory);
            }
            else
            {
                FileBlock newFile = CreateFileObject(item, directory);
                if (directory.Files == null)
                    directory.Files = new List<FileBlock>();
                directory.Files.Add(newFile);
            }
        }

        internal bool isDirectory(String fileType)
        {
            if (fileType.Equals("folder"))
            {
                return true;
            }
            else
                return false;
        }

        internal DirectoryBlock CreateDirectoryObject(Box.V2.Models.BoxItem item, DirectoryBlock parentDirectory)
        {
            string parent = null;
            if (item.Parent == null)
            {
                parent = "";
            }

            DirectoryBlock directory = new DirectoryBlock()
            {
                id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Size = item.Size,
                SharedLink = GetSharedLink(item),
                Owners = GetOwners(item),
                CreationTime = item.CreatedAt.ToString(),
                Users = GetUsers(item),
                Parent = new List<string> { parent },
                path = parentDirectory.path + "\\" + item.Name,
            };
            return directory;
        }

        internal FileBlock CreateFileObject(Box.V2.Models.BoxItem item, DirectoryBlock parentDirectory)
        {
            string parent = null;
            if (item.Parent == null)
                parent = "";
            FileBlock file = new FileBlock()
            {
                id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Size = item.Size,
                SharedLink = GetSharedLink(item),
                Owners = GetOwners(item),
                CreationTime = item.CreatedAt.ToString(),
                // Users = GetUsers(item),
                Parent = new List<string> { parent },
                path = parentDirectory.path + "\\" + item.Name,
            };
            return file;
        }

        internal String GetSharedLink(Box.V2.Models.BoxItem item)
        {
            String url = null;
            if (item.SharedLink != null)
                url = item.SharedLink.Url;
            return url;
        }

        internal List<Owner> GetOwners(Box.V2.Models.BoxItem item)
        {
            List<Owner> owners = new List<Owner>();
            Owner owner = new Owner()
            {
                DisplayName = item.OwnedBy.Name,
                Email = item.OwnedBy.Login,
            };
            owners.Add(owner);
            return owners;
        }

        internal List<User> GetUsers(Box.V2.Models.BoxItem item)
        {
            List<User> Users = null;
            try
            {
                Task<BoxCollection<BoxCollaboration>> task = Task.Run(() => client.FoldersManager.GetCollaborationsAsync(item.Id));
                var collaborations = task.Result;
                if (collaborations.Entries.Count > 0)
                {
                    Users = new List<User>();
                    // Add users and permissions
                    foreach (var collaboration in collaborations.Entries)
                    {
                        User newUser = CreateUserObject(collaboration);
                        Users.Add(newUser);
                    }
                }
            }
            catch (BoxException e)
            {
                throw new Exception(e.Message + " " + e.StackTrace);
            }
            return Users;
        }

        internal User CreateUserObject(Box.V2.Models.BoxCollaboration collaboration)
        {
            User user = new User()
            {
                DisplayName = collaboration.AccessibleBy.Id,
                //EmailAddress = userInfo.Login, 
                Permissions = GetPermissions(collaboration),
            };
            return user;
        }

        internal List<Permission> GetPermissions(Box.V2.Models.BoxCollaboration collab)
        {
            List<Permission> permissions = new List<Permission>();
            Permission permission = new Permission();
            permission.PermissionType = collab.Role;
            permissions.Add(permission);
            return permissions;
        }

        public DirectoryBlock GetRoot()
        {
            DirectoryBlock root = new DirectoryBlock()
            {
                id = sourceFolder,
            };
            return root;
        }

        private bool UploadFolder(FileBlock item)
        {
            try
            {
                BoxFolderRequest request = new BoxFolderRequest()
                {
                    Name = item.Name,
                    Parent = new BoxRequestEntity
                    {
                        Id = "0",
                    }
                };
                Task<BoxFolder> task = Task.Run(() => client.FoldersManager.CreateAsync(request));
                var file = task.Result;
            }
            catch (BoxException e)
            {
                throw e;
            }
            return true;
        }

        public void UploadDirectory(DirectoryBlock item)
        {
            throw new NotImplementedException();
        }

        public long GetTotalSpace()
        {
            Task<BoxUser> currentUser = Task.Run(() => client.UsersManager.GetCurrentUserInformationAsync());
            currentUser.Wait();
            Task<BoxUser> userInfo = client.UsersManager.GetUserInformationAsync(currentUser.Result.Id.ToString());
            userInfo.Wait();
            return (long)userInfo.Result.SpaceAmount;
        }

        public long GetUsedSpace()
        {
            Task<BoxUser> currentUser = Task.Run(() => client.UsersManager.GetCurrentUserInformationAsync());
            currentUser.Wait();
            Task<BoxUser> userInfo = client.UsersManager.GetUserInformationAsync(currentUser.Result.Id.ToString());
            userInfo.Wait();
            return (long)userInfo.Result.SpaceUsed;
        }


        public Task<string> GetRootId()
        {
            throw new NotImplementedException();
        }

        public async Task createSharedLink()
        {
            try
            {
                var items = await client.FoldersManager.GetFolderItemsAsync("0", 500, fields: new string[]
                        { "id", "name"});
                foreach (var item in items.Entries)
                {
                    Console.WriteLine(item.Id + item.Name);
                }

                BoxSharedLinkRequest request = new BoxSharedLinkRequest()
                {
                    Access = BoxSharedLinkAccessType.open,
                };
                var link = await client.FoldersManager.CreateSharedLinkAsync("30787703220", request);
                var link2 = await client.FilesManager.CreateSharedLinkAsync("707877453", request);
                Console.WriteLine(link.SharedLink);
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        public bool UploadFile(FileBlock file, string destinationPath = null)
        {
            bool status = false;
            try
            {
                using (var stream = new FileStream(file.path, FileMode.OpenOrCreate))
                {
                    BoxFileRequest request = new BoxFileRequest()
                    {
                        Name = file.id,
                        Parent = new BoxRequestEntity() { Id = file.Parent[0] }
                    };
                    Task task = Task.Run(() => client.FilesManager.UploadAsync(request, stream));
                    task.Wait();
                    status = true;
                }
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

        public void DownloadFile(FileBlock file, string localPath)
        {
            try
            {
                Task<Stream> task = Task.Run(() => client.FilesManager.DownloadStreamAsync(file.id));
                var responseStream = task.Result;
                using (var outstream = new FileStream(localPath + file.path, FileMode.OpenOrCreate))
                {
                    responseStream.CopyTo(outstream);
                }
                responseStream.Dispose();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
