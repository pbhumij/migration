using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Migration.Common;
using Migration.Resource;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Migration.CloudService.GoogleDriveService
{
    /// <summary>
    /// Google Drive specific collection of methods.  
    /// </summary>
    public class GoogleDriveService : ICloudServiceIO
    {

        public string GetRootId()
        {
            return "root";
        }

        /// <summary>
        /// This Method takes in a parent directory as parameter.
        /// Lists all the sub-directories and adds them to the parent directory. 
        /// Lists all the files and adds them to the parent directory. 
        /// </summary>
        public void GetFiles(DirectoryBlock parentDirectory)
        {
            using (DriveService service = DriveClient.GetInfo())
            {
                try
                {
                    FilesResource.ListRequest listRequest = service.Files.List();
                    listRequest.Q = "'" + parentDirectory.id + "' in parents and trashed = false";
                    listRequest.OrderBy = "name";
                    listRequest.Fields = "nextPageToken, files" +
                        "(id, name, mimeType, permissions, webContentLink, description, size, shared, owners, parents)";
                    Google.Apis.Drive.v3.Data.FileList ListResponse = listRequest.Execute();
                    while (ListResponse.Files != null && ListResponse.Files.Count != 0)
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (var item in ListResponse.Files)
                        {
                            tasks.Add(Task.Run(() => AddToParentDirectoryObject(item, parentDirectory)));
                        }
                        Task.WaitAll(tasks.ToArray());
                        if (ListResponse.NextPageToken == null)
                            break;
                        listRequest.PageToken = ListResponse.NextPageToken;
                        ListResponse = listRequest.Execute();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception(e.StackTrace);
                }
            }

        }

        // Checks whether a file is of directory type.
        internal bool isDirectory(String FileMimeType)
        {
            if (FileMimeType.Equals("application/vnd.google-apps.folder"))
                return true;
            else
                return false;
        }

        internal void AddToParentDirectoryObject(Google.Apis.Drive.v3.Data.File item, DirectoryBlock directory)
        {
            if (isDirectory(item.MimeType))
            {
                if (directory.SubDirectories == null)
                    directory.SubDirectories = new List<DirectoryBlock>();
                directory.SubDirectories.Add(CreateDirectoryObject(item, directory));
            }
            else
            {
                if (directory.Files == null)
                    directory.Files = new List<FileBlock>();
                directory.Files.Add(CreateFileObject(item, directory));
            }
        }

        internal DirectoryBlock CreateDirectoryObject(Google.Apis.Drive.v3.Data.File item, DirectoryBlock parentDirectory)
        {
            DirectoryBlock directory = new DirectoryBlock()
            {
                id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Size = item.Size,
                SharedLink = GetSharedLink(item),
                Owners = GetOwners(item),
                CreationTime = item.CreatedTime.ToString(),
                Users = GetUsers(item),
                path = parentDirectory.path + "\\" + item.Name,
                mimeType = "application/folder",
                Parent = (List<string>)item.Parents,
            };
            return directory;
        }

        internal FileBlock CreateFileObject(Google.Apis.Drive.v3.Data.File item, DirectoryBlock parentDirectory)
        {
            FileBlock file = new FileBlock()
            {
                id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Size = item.Size,
                SharedLink = item.WebContentLink,
                Owners = GetOwners(item),
                CreationTime = item.CreatedTime.ToString(),
                Users = GetUsers(item),
                path = parentDirectory.path +"\\" + item.Name,
                mimeType = item.MimeType,
                Parent = (List<string>)item.Parents,
            };
            return file;
        }

        internal String GetSharedLink(Google.Apis.Drive.v3.Data.File item)
        {
            if ((bool)item.Shared)
            {
                var url = item.WebContentLink;
                return url;
            }
            else
            {
                return null;
            }
        }

        internal List<Owner> GetOwners(Google.Apis.Drive.v3.Data.File item)
        {
            List<Owner> owners = new List<Owner>();
            foreach (var owner in item.Owners)
            {
                Owner newOwner = new Owner()
                {
                    DisplayName = owner.DisplayName,
                    Email = owner.EmailAddress,
                };
                owners.Add(newOwner);
            }
            return owners;
        }

        /// <summary>
        /// This method takes a Google Drive file or folder as parameter
        /// and returns a list of users along with their access permissions type.
        /// </summary>
        internal List<Resource.User> GetUsers(Google.Apis.Drive.v3.Data.File file)
        {
            List<Resource.User> Users = null;
            // Add users and permissions
            IList<Google.Apis.Drive.v3.Data.Permission> permissions = file.Permissions;
            if (permissions != null && permissions.Count > 0)
            {
                Users = new List<Resource.User>();
                foreach (Google.Apis.Drive.v3.Data.Permission permission in permissions)
                {
                    Resource.User user = CreateUserObject(permission);
                    if (user != null)
                        Users.Add(user);
                }
            }
            return Users;
        }

        internal Resource.User CreateUserObject(Google.Apis.Drive.v3.Data.Permission permission)
        {
            Resource.User user = null;
            if (permission.DisplayName != null)
            {
                user = new Resource.User()
                {
                    DisplayName = permission.DisplayName,
                    EmailAddress = permission.EmailAddress,
                    Permissions = GetPermissions(permission),
                };
            }
            return user;
        }

        internal List<Resource.Permission> GetPermissions(Google.Apis.Drive.v3.Data.Permission permission)
        {
            List<Resource.Permission> permissions = new List<Resource.Permission>();
            Resource.Permission userPermission = new Resource.Permission();
            userPermission.PermissionType = permission.Role.ToString();
            permissions.Add(userPermission);
            return permissions;
        }
      
        //public bool UploadFile(Containers.File item)
        //{
        //    bool status = false;
        //    try
        //    {
        //        var fileMetadata = new Google.Apis.Drive.v3.Data.File()
        //        {
        //            Name = item.Name,
        //            Parents = item.Parent,
        //        };
        //        FilesResource.CreateMediaUpload request;
        //        using (var service = DriveClient.GetInfo())
        //        {
        //            request = service.Files.Create(
        //            fileMetadata, new FileStream(item.path, FileMode.Open), item.mimeType);
        //            request.Upload();
        //        }
        //        status = true;
        //    }
        //    catch (Exception e)
        //    {
        //        throw e;
        //    }
        //    return status;
        //}

        internal bool UploadDirectory(DirectoryBlock item)
        {
            bool status = false;
            try
            {
                using (var service = DriveClient.GetInfo())
                {
                    var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = item.Name,
                        MimeType = "application/vnd.google-apps.folder",
                        Parents = item.Parent,
                    };
                    var request = service.Files.Create(fileMetadata).Execute();
                    status = true;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return status;
        }

        internal string getDirectoryPath(string downloadPath)
        {
            string path = downloadPath.Substring(0, (downloadPath.LastIndexOf('\\')));
            return path;
        }

        public long GetTotalSpace()
        {
            using (var client = DriveClient.GetInfo())
            {
                Google.Apis.Drive.v3.AboutResource.GetRequest request = client.About.Get();
                request.Fields = "storageQuota";
                var response = request.Execute();
                return (long)response.StorageQuota.Limit;
            }
        }

        public long GetUsedSpace()
        {
            using (var client = DriveClient.GetInfo())
            {
                Google.Apis.Drive.v3.AboutResource.GetRequest request = client.About.Get();
                request.Fields = "storageQuota";
                var response = request.Execute();
                return (long)response.StorageQuota.UsageInDrive;
            }
        }

        public bool UploadCloudFile(FileBlock file, string destinationPath = null)
        {
            bool status = false;
            try
            {
                using (var client = DriveClient.GetInfo())
                {
                    using (var stream = new FileStream(file.path, FileMode.OpenOrCreate))
                    {
                        FilesResource.CreateMediaUpload request = new FilesResource.CreateMediaUpload(client,
                           new Google.Apis.Drive.v3.Data.File
                           {
                               Name = file.Name,
                               Parents = file.Parent,
                           },
                           stream,
                           file.mimeType);
                        request.ChunkSize = 1024 * 1024;
                        Task initiateSessionTask = Task.Run(() => request.InitiateSessionAsync());
                        initiateSessionTask.Wait();
                        Task<Google.Apis.Upload.IUploadProgress> uploadTask = Task.Run(() => request.UploadAsync());
                        uploadTask.Wait();
                        status = true;
                    }                      
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " " + e.StackTrace);
            }

            return status;
        }

        internal string CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            return null;
        }

        public void DownloadCloudFile(FileBlock file, string localPath)
        {
            try
            {
                using (var client = DriveClient.GetInfo())
                {
                    string fileFullPath = localPath + file.path;
                    string ext = GetExt(file.mimeType);
                    var downloadRequest = client.Files.Get(file.id);
                    using (FileStream stream = new FileStream(fileFullPath, FileMode.OpenOrCreate))
                    {
                        downloadRequest.Download(stream);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal string GetExt(string mimeType)
        {
            int index = mimeType.LastIndexOf("/");
            string ext = mimeType.Substring(index+1);
            return ext;
        }

        string ICloudServiceIO.CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            throw new NotImplementedException();
        }
    }
}

