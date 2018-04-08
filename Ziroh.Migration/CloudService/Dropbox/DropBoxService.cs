using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using Migration.Common;
using Migration.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.CloudService.DropboxService
{
    public class DropBoxService : ICloudServiceIO
    {
        public string sourceFolder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string GetRootId()
        {
            return "";
        }

        internal async Task AddToParentDirectory(Dropbox.Api.Files.Metadata item, DirectoryBlock directory)
        {
            if (item.IsFolder && !item.IsDeleted)
            {
                if (directory.SubDirectories == null)
                    directory.SubDirectories = new List<DirectoryBlock>();
                directory.SubDirectories.Add(await CreateDirectoryObject(item));
            }
            else if (item.IsFile)
            {
                if (directory.Files == null)
                    directory.Files = new List<FileBlock>();
                directory.Files.Add(await CreateFileObject(item));
            }
        }

        internal async Task<DirectoryBlock> CreateDirectoryObject(Dropbox.Api.Files.Metadata item)
        {
            DirectoryBlock directory = new DirectoryBlock()
            {
                id = item.AsFolder.Id,
                Name = item.AsFolder.Name,
                SharedLink = await GetSharedLink(item),
                Owners = await GetOwners(item),
                Users = await GetUsers(item),
                path = item.PathDisplay,
            };
            return directory;
        }

        internal async Task<FileBlock> CreateFileObject(Dropbox.Api.Files.Metadata item)
        {
            FileBlock file = new FileBlock()
            {
                id = item.AsFile.Id,
                Name = item.AsFile.Name,
                Size = (long)item.AsFile.Size,
                SharedLink = await GetSharedLink(item),
                Owners = await GetOwners(item),
                CreationTime = item.AsFile.ClientModified.ToString(),
                Users = await GetUsers(item),
                path = item.PathDisplay,
            };
            return file;
        }

        internal async Task<String> GetSharedLink(Dropbox.Api.Files.Metadata item)
        {
            String url = null;
            if (item.PathDisplay != null)
            {
                if (item.IsFolder)
                {
                    url = await GetFolderUrl(item);
                }
                if (item.IsFile)
                {
                    url = await GetFileUrl(item);
                }
            }

            return url;
        }

        internal async Task<String> GetFolderUrl(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                String url = null;
                ListSharedLinksResult result = await client.Sharing.ListSharedLinksAsync(item.AsFolder.PathDisplay);
                Parallel.ForEach(result.Links, (data, loopState) =>
                {
                    if (data.PathLower == item.AsFolder.PathLower)
                    {
                        url = data.Url;
                        loopState.Stop();
                    }
                });
                return url;
            }
        }

        internal async Task<String> GetFileUrl(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                String url = null;
                ListSharedLinksResult result = await client.Sharing.ListSharedLinksAsync(item.AsFile.PathDisplay);
                Parallel.ForEach(result.Links, (data, loopState) =>
                {
                    if (data.PathLower == item.AsFile.PathLower)
                    {
                        url = data.Url;
                        loopState.Stop();
                    }
                });
                return url;
            }
        }

        internal async Task<List<User>> GetUsers(Dropbox.Api.Files.Metadata item)
        {
            List<User> users = null;
            if (item.IsFolder && item.AsFolder.SharingInfo.SharedFolderId != null)
            {
                users = await GetFolderUsers(item);
            }
            if (item.IsFile && item.AsFile.SharingInfo != null)
            {
                users = await GetFileUsers(item);
            }
            return users;
        }

        internal async Task<List<User>> GetFolderUsers(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                List<User> users = null;
                SharedFolderMembers members = await client.Sharing.ListFolderMembersAsync(item.AsFolder.SharingInfo.SharedFolderId);
                users = new List<User>();
                var list_continue = false;
                do
                {
                    foreach (var member in members.Users)
                    {
                        User user = await CreateUserObject(member);
                        users.Add(user);
                    }
                    list_continue = (members.Cursor != null);
                    if (list_continue)
                        members = await client.Sharing.ListFolderMembersContinueAsync(item.AsFolder.SharingInfo.SharedFolderId);
                }
                while (list_continue);
                return users;
            }
        }

        internal async Task<List<User>> GetFileUsers(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                List<User> users = null;
                SharedFileMembers members = await client.Sharing.ListFileMembersAsync(item.AsFile.Id);
                users = new List<User>();
                var list_continue = false;
                do
                {
                    foreach (var member in members.Users)
                    {
                        User user = await CreateUserObject(member);
                        users.Add(user);
                    }
                    list_continue = (members.Cursor != null);
                    if (list_continue)
                        members = await client.Sharing.ListFileMembersContinueAsync(item.AsFile.Id);
                }
                while (list_continue);
                return users;
            }
        }

        internal async Task<User> CreateUserObject(UserMembershipInfo member)
        {
            using (var client = DropBoxClient.GetClient())
            {
                var userInfo = await client.Users.GetAccountAsync(member.User.AccountId);
                User user = new User()
                {
                    DisplayName = userInfo.Name.DisplayName,
                    EmailAddress = GetEmail(userInfo),
                    Permissions = GetPermissions(member),
                };
                return user;
            }
        }

        internal string GetEmail(Dropbox.Api.Users.BasicAccount userInfo)
        {
            if (userInfo.EmailVerified)
                return userInfo.Email;
            else
                return null;
        }

        internal List<Permission> GetPermissions(UserMembershipInfo member)
        {
            List<Permission> permissions = new List<Permission>();
            Permission permission = new Permission();
            permission.PermissionType = member.AccessType.ToString();
            permissions.Add(permission);
            return permissions;
        }

        internal async Task<List<Owner>> GetOwners(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                List<Owner> owners = null;
                if (item.IsFolder && item.AsFolder.SharingInfo.SharedFolderId != null)
                {
                    owners = await GetFolderOwners(item);
                }
                if (item.IsFile && item.AsFile.SharingInfo != null)
                {
                    owners = await GetFileOwners(item);
                }
                return owners;
            }
        }

        internal async Task<List<Owner>> GetFolderOwners(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                List<Owner> owners = null;
                var list = await client.Sharing.ListFolderMembersAsync(item.AsFolder.SharingInfo.SharedFolderId);
                owners = new List<Owner>();
                foreach (var member in list.Users)
                {
                    if (member.AccessType.IsOwner)
                    {
                        var ownerInfo = await client.Users.GetAccountAsync(member.User.AccountId);
                        Owner owner = new Owner()
                        {
                            DisplayName = ownerInfo.Name.DisplayName,
                            Email = GetEmail(ownerInfo),
                        };
                        owners.Add(owner);
                    }
                }
                return owners;
            }
        }

        internal async Task<List<Owner>> GetFileOwners(Dropbox.Api.Files.Metadata item)
        {
            using (var client = DropBoxClient.GetClient())
            {
                List<Owner> owners = null;
                var list = await client.Sharing.ListFileMembersAsync(item.AsFile.Id);
                owners = new List<Owner>();
                foreach (var member in list.Users)
                {
                    if (member.AccessType.Equals("owner"))
                    {
                        Owner owner = new Owner()
                        {
                            DisplayName = member.User.AccountId,
                        };
                    }
                }
                return owners;
            }
        }

        public long GetTotalSpace()
        {
            using (var client = DropBoxClient.GetClient())
            {
                try
                {
                    Task<Dropbox.Api.Users.SpaceUsage> info = Task.Run(() => client.Users.GetSpaceUsageAsync());
                    ulong value = info.Result.Allocation.AsIndividual.Value.Allocated;
                    return Convert.ToInt64(value);
                }
                catch (Exception e)
                {

                    throw e;
                }
            }
        }

        public long GetUsedSpace()
        {
            using (var client = DropBoxClient.GetClient())
            {
                Task<Dropbox.Api.Users.SpaceUsage> info = Task.Run(() => client.Users.GetSpaceUsageAsync());
                ulong value = info.Result.Used;
                return Convert.ToInt64(value);
            }
        }

        public void GetFiles(DirectoryBlock parentDirectory)
        {
            using (var client = DropBoxClient.GetClient())
            {
                try
                {
                    Task<ListFolderResult> GetListTask = Task.Run(() => client.Files.ListFolderAsync(parentDirectory.id));
                    var items = GetListTask.Result;
                    var list_continue = false;
                    if (items != null)
                    {
                        do
                        {
                            foreach (var item in items.Entries)
                            {
                                Task task = Task.Run(() => AddToParentDirectory(item, parentDirectory));
                                task.Wait();
                            }
                            if (items.HasMore)
                                list_continue = (items.Cursor != null);
                            if (list_continue)
                            {
                                items = Task.Run(() => client.Files.ListFolderContinueAsync(parentDirectory.id)).Result;
                            }
                        }
                        while (list_continue);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }

        public bool UploadCloudFile(FileBlock file, string destinationPath = null)
        {
            bool status = false;
            try
            {
                using (var client = DropBoxClient.GetClient())
                {
                    Task task = Task.Run(() => client.Files.UploadAsync(file.path));
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

        public void DownloadCloudFile(FileBlock file, string localPath)
        {
            using (var client = DropBoxClient.GetClient())
            {
                try
                {
                    Task<Dropbox.Api.Stone.IDownloadResponse<FileMetadata>> task = Task.Run(() => client.Files.DownloadAsync(file.id));
                    var metadataResponse = task.Result;
                    Task<Stream> stream = Task.Run(() => metadataResponse.GetContentAsStreamAsync());
                    var streamResponse = stream.Result;
                    using (var outStream = new FileStream(localPath + file.path, FileMode.OpenOrCreate))
                    {
                        streamResponse.CopyTo(outStream);
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }


        string ICloudServiceIO.CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            throw new NotImplementedException();
        }
    }
}

