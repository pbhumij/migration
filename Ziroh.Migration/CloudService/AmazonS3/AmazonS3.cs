using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using Migration.Common;
using Migration.Resource;

namespace Migration.CloudService.AmazonS3
{
    /// <summary>
    /// Amazon S3 specific collection of methods.
    /// </summary>
    public class AmazonS3 : ICloudServiceIO
    {
        public string sourceFolder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// This Method takes in a parent directory as parameter.
        /// Lists all the sub-directories and adds them to the parent directory. 
        /// Lists all the files and adds them to the parent directory. 
        /// </summary>
        public void GetFiles(DirectoryBlock parentDirectory)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                try
                {
                    if (parentDirectory.id == "root")
                    {
                        parentDirectory.SubDirectories = GetS3Buckets();
                    }
                    else if(string.IsNullOrEmpty(parentDirectory.prefix))
                    {
                        GetObjects(parentDirectory);
                        GetBucketLevelPseudoDirectories(parentDirectory);
                    }
                    else
                    {
                        GetObjects(parentDirectory);
                        GetNestedPseudoDirectories(parentDirectory);
                    }
                }
                catch (AmazonS3Exception e)
                {
                    throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
                }
            }
        }

        private void GetNestedPseudoDirectories(DirectoryBlock parentDirectory)
        {
            using(AmazonS3Client client = S3Credential.GetClient())
            {
                ListObjectsV2Request request = new ListObjectsV2Request()
                {
                    BucketName = parentDirectory.BucketName,
                    Prefix = parentDirectory.prefix,
                    MaxKeys = 100,
                };

                Task<ListObjectsV2Response> responseTask = Task.Run(() => client.ListObjectsV2Async(request));
                ListObjectsV2Response response = responseTask.Result;

                foreach(S3Object obj in response.S3Objects)
                {
                    if (!(obj.Key == parentDirectory.prefix) && obj.Key.EndsWith("/"))
                    {
                        obj.Key = obj.Key.Substring(parentDirectory.prefix.Length);
                        obj.Key = obj.Key.Substring(0, obj.Key.IndexOf("/"));
                        if (parentDirectory.SubDirectories == null)
                            parentDirectory.SubDirectories = new List<DirectoryBlock>();
                            parentDirectory.SubDirectories.Add(CreatePseudoDirectoryObject(parentDirectory, obj));
                    }
                }
            }
        }

        private void GetObjects(DirectoryBlock parentDirectory)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                ListObjectsV2Request request = new ListObjectsV2Request()
                {
                    BucketName = parentDirectory.BucketName,
                    Prefix = parentDirectory.prefix,
                    Delimiter = "/",
                };
                Task<ListObjectsV2Response> responseTask = Task.Run(() => client.ListObjectsV2Async(request));
                ListObjectsV2Response response = responseTask.Result;
                if(response.S3Objects.Count > 0)
                {
                    do
                    {
                        foreach (S3Object obj in response.S3Objects)
                        {
                            if (!(obj.Key.EndsWith("/")))
                            {
                                if (parentDirectory.Files == null)
                                    parentDirectory.Files = new List<FileBlock>();
                                parentDirectory.Files.Add(CreateFileObject(parentDirectory, obj));
                            }
                        }
                        request.ContinuationToken = response.NextContinuationToken;
                    }
                    while (response.IsTruncated);
                }
            }       
        }

        // Get Directories
        private List<DirectoryBlock> GetS3Buckets()
        {
            List<DirectoryBlock> list = null;
            try
            {
                using (AmazonS3Client client = S3Credential.GetClient())
                {
                    ListBucketsResponse response = client.ListBuckets();
                    if (response.Buckets.Count > 0)
                    {
                        list = new List<DirectoryBlock>();
                        foreach (S3Bucket bucket in response.Buckets)
                        {
                            DirectoryBlock directory = CreateDirectoryObject(bucket);
                            list.Add(directory);
                        }
                    }
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
            }
            return list;
        }

        private void GetBucketLevelPseudoDirectories(DirectoryBlock parentDirectory)
        {
            try
            {
                ListObjectsV2Request request = new ListObjectsV2Request()
                {
                    BucketName = parentDirectory.BucketName,
                    MaxKeys = 1000,
                    Prefix = parentDirectory.prefix,
                };
                ListObjectsV2Response response = ListObjects(request);
                if (response.S3Objects.Count > 0)
                {
                    do
                    {
                        foreach (S3Object s3Obj in response.S3Objects)
                        {
                            
                            // Pseudo directories inside bucket
                            if (s3Obj.Key.IndexOf('/') == s3Obj.Key.LastIndexOf('/') && s3Obj.Key != parentDirectory.prefix && s3Obj.Key.EndsWith("/"))
                            {
                                if (parentDirectory.SubDirectories == null)
                                    parentDirectory.SubDirectories = new List<DirectoryBlock>();
                                 parentDirectory.SubDirectories.Add(CreatePseudoDirectoryObject(parentDirectory, s3Obj));
                            }
                            if(s3Obj.Key != parentDirectory.prefix && s3Obj.Key.EndsWith("/"))
                            {

                            }
                            //// pseudo objects inside pseudo directories
                            //else if(!(string.IsNullOrEmpty(parentDirectory.prefix)) && s3Obj.Key != parentDirectory.prefix)
                            //{
                            //    parentDirectory.Files.Add(CreatePseudoFileObject(parentDirectory, s3Obj));
                            //}
                        }
                        request.ContinuationToken = response.NextContinuationToken;
                    } while (response.IsTruncated);
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
            }
        }

        private DirectoryBlock CreatePseudoDirectoryObject(DirectoryBlock parentDirectory, S3Object s3Obj)
        {
            string name = s3Obj.Key;
            if(s3Obj.Key.Contains("/"))
                 name = s3Obj.Key.Substring(0, s3Obj.Key.IndexOf("/"));
            DirectoryBlock newDirectory = new DirectoryBlock()
            {
                BucketName = parentDirectory.BucketName,
                id = name,
                Name = name,
                prefix = parentDirectory.prefix + name + "/",
                path = parentDirectory.path +"/"+ name,
            };
            return newDirectory;
        }

        private string GetName(string key, DirectoryBlock parentDirectory)
        {
            // for pseudo-directory
            string name = key.Substring(parentDirectory.prefix.Length);
            if (name.Contains("/"))
                name = name.Substring(0, name.Length-1);
            return name;
        }

        DirectoryBlock CreateDirectoryObject(S3Bucket bucket)
        {
            DirectoryBlock directory = new DirectoryBlock()
            {
                Name = bucket.BucketName,
                Users = GetUsers(bucket),
                CreationTime = bucket.CreationDate.ToString(),
                BucketName = bucket.BucketName,
                prefix = "",
                path = bucket.BucketName,
            };
            return directory;
        }

        FileBlock CreateFileObject(DirectoryBlock parentDirectory, S3Object obj)
        {
            string name = obj.Key;
            if (obj.Key.Contains("/"))
                name = obj.Key.Substring(obj.Key.LastIndexOf("/")+1);
            FileBlock file = new FileBlock()
            {
                BucketName = parentDirectory.BucketName,
                id = name,
                Name = name,
                Size = obj.Size,
                Users = GetUsers(parentDirectory.BucketName, obj),
                path = parentDirectory.path + "/"+name,
                prefix = parentDirectory.prefix + name,
                //SharedLink = GetSharedLink(bucketName, key),
            };
            return file;
        }

        // Get users for bucket
        private List<User> GetUsers(S3Bucket bucket)
        {
            List<User> Users = null;
            try
            {
                List<S3Grant> Grants = ListUsers(bucket.BucketName);
                if (Grants.Count > 0)
                {
                    Users = new List<User>();
                    foreach (S3Grant grant in Grants)
                    {
                        User user = CreateUserObject(grant);
                        Users.Add(user);
                    }
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
            }
            return Users;
        }

        // Get Users of S3 objects
        private List<User> GetUsers(String bucketName, S3Object obj)
        {
            List<User> Users = null;
            try
            {
                List<S3Grant> Grants = ListUsers(bucketName, obj.Key);
                if (Grants.Count > 0)
                {
                    Users = new List<User>();
                    foreach (S3Grant grant in Grants)
                    {
                        User user = CreateUserObject(grant);
                        Users.Add(user);
                    }
                }
            }
            catch (AmazonS3Exception e)
            {
                throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
            }
            return Users;
        }

        internal User CreateUserObject(S3Grant grant)
        {
            User user = new User();
            user.DisplayName = grant.Grantee.DisplayName;
            user.EmailAddress = grant.Grantee.EmailAddress;
            user.Permissions = GetPermissions(grant);
            return user;
        }

        internal List<Permission> GetPermissions(S3Grant grant)
        {
            List<Permission> permissions = new List<Permission>();
            Permission permission = new Permission();
            permission.PermissionType = grant.Permission;
            permissions.Add(permission);
            return permissions;
        }

        public async Task<String> NewBucket(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                String result = null;
                try
                {
                    PutBucketRequest putRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };
                    bool found = await IfBucketExist(bucketName);
                    if (!found)
                    {
                        PutBucketResponse response = await client.PutBucketAsync(putRequest);
                        EnableVersioningOnBucket(bucketName);
                        result = response.HttpStatusCode.ToString();
                    }
                    else
                        result = "Bucket name already exist";
                }
                catch (AmazonS3Exception ex)
                {
                    result = ex.Message.ToString();
                }
                return result;
            }
        }

        public async Task<String> DeleteBucket(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                String result = null;
                try
                {
                    DeleteBucketRequest deleteBucketRequest = new DeleteBucketRequest
                    {
                        BucketName = bucketName
                    };
                    bool found = await IfBucketExist(bucketName);
                    if (!found)
                    {
                        return "Bucket Not Found";
                    }
                    else
                    {
                        bool isBucketEmpty = await DeleteObjects(bucketName);
                        if (isBucketEmpty)
                        {
                            DeleteBucketResponse deleteBucketResponse = await client.DeleteBucketAsync(deleteBucketRequest);
                            result = deleteBucketResponse.HttpStatusCode.ToString();
                            // NoContent indicates that the request has been successfully processed and that the response is intentionally blank.
                            if (result == "NoContent")
                            {
                                result = "OK";
                            }
                        }
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    result = ex.Message.ToString();
                }
                return result;
            }
        }

        //public FileChunkListContainer UploadFile(String filePath, String bucketName)
        //{
        //    FileChunkListContainer container = new FileChunkListContainer();
        //    SetInitialContainerProperties(container, filePath);
        //    if (System.IO.File.Exists(filePath))
        //    {
        //        try
        //        {
        //            MemoryMappedFile memoryMappedFile = memoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "mappedfile");
        //            MemoryMappedViewAccessor accessor = null;
        //            Transfer transferObj = new Transfer();
        //            transferObj.TransferStatus = true;
        //            while (transferObj.TransferStatus)
        //            {
        //                if (!transferObj.ResumeStatus)
        //                {
        //                    accessor = memoryMappedFile.CreateViewAccessor();
        //                    FileInfo file = new FileInfo(filePath);
        //                    int length = (int)file.Length;
        //                    int totalChunks = (length / (1024 * 1024)) + 1;
        //                    Parallel.For(0, totalChunks, (j) =>
        //                    {
        //                        FileChunkObject chunk = new FileChunkObject();
        //                        SetInitialChunkProperties(chunk);
        //                        chunk.Offset = 1024 * 1024 * j;
        //                        UploadChunk(accessor, bucketName, chunk).Wait();
        //                        container.ChunkList.Add(chunk);
        //                    });
        //                    accessor.Dispose();
        //                    UpdateContainerProperties(container);
        //                    transferObj.UpdateResumeStatus(container.ChunkList);
        //                }
        //                else
        //                {
        //                    Console.WriteLine("resuming upload..");
        //                    Parallel.ForEach(container.ChunkList, (FileChunkObject chunk) =>
        //                    {
        //                        if (!chunk.ChunkStatus)
        //                        {
        //                            accessor = memoryMappedFile.CreateViewAccessor();
        //                            UploadChunk(accessor, bucketName, chunk).Wait();
        //                        }
        //                    });
        //                    UpdateContainerProperties(container);
        //                    transferObj.UpdateResumeStatus(container.ChunkList);

        //                }
        //                transferObj.UpdateTransferStatus(container.ChunkList);
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            throw new Exception(e.Message);
        //        }
        //    }
        //    return container;
        //}

        //public Boolean DownloadFile(FileChunkListContainer container, String downloadLocation, String bucketName)
        //{
        //    Boolean downloadStatus = false;
        //    int sizeOfFileToDownload = container.FileSize;
        //    SetInitialContainerProperties(container);
        //    string downloadPath = downloadLocation + container.CloudFileName;
        //    if (System.IO.File.Exists(downloadPath))
        //    {
        //        downloadPath = @"C:\\Users\\Quarx\\Music\\" + container.CloudFileName + "copy";
        //    }
        //    try
        //    {
        //        MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(downloadPath, FileMode.CreateNew, "log-map", sizeOfFileToDownload);
        //        Transfer transferObj = new Transfer();
        //        transferObj.TransferStatus = true;
        //        while (transferObj.TransferStatus)
        //        {
        //            if (!transferObj.ResumeStatus)
        //            {
        //                Parallel.ForEach(container.ChunkList, (FileChunkObject chunk) =>
        //                {
        //                    DownloadChunk(memoryMappedFile, bucketName, chunk).Wait();

        //                });
        //                Parallel.ForEach(container.ChunkList, (FileChunkObject chunk) =>
        //                {
        //                    Console.WriteLine("offset: " + chunk.Offset + " status: " + chunk.ChunkStatus + " size: " + chunk.Size);
        //                });
        //                UpdateContainerProperties(container);
        //                transferObj.UpdateResumeStatus(container.ChunkList);
        //            }
        //            else
        //            {
        //                Parallel.ForEach(container.ChunkList, (FileChunkObject chunk) =>
        //                {
        //                    if (!chunk.ChunkStatus)
        //                    {
        //                        DownloadChunk(memoryMappedFile, bucketName, chunk).Wait();

        //                    }
        //                });
        //                UpdateContainerProperties(container);
        //                transferObj.UpdateResumeStatus(container.ChunkList);
        //            }
        //            transferObj.UpdateTransferStatus(container.ChunkList);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }

        //    downloadStatus = container.status;
        //    return downloadStatus;
        //}

        public async Task<ListBucketsResponse> ListBuckets()
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                ListBucketsResponse list = null;
                try
                {
                    list = await client.ListBucketsAsync();
                }
                catch (AmazonS3Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return list;
            }
        }

        public ListObjectsV2Response ListObjects(ListObjectsV2Request request)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                ListObjectsV2Response response = null;
                try
                {
                    response = client.ListObjectsV2(request);
                }
                catch (AmazonS3Exception ex)
                {
                    throw new AmazonS3Exception(ex.Message + " " + ex.StackTrace);
                }
                return response;
            }
        }

        // For object
        public List<S3Grant> ListUsers(String bucketName, String fileName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                List<S3Grant> listOfGrants = null;
                try
                {
                    S3AccessControlList acl = client.GetACL(new GetACLRequest
                    {
                        BucketName = bucketName,
                        Key = fileName
                    }).AccessControlList;
                    listOfGrants = acl.Grants;
                }
                catch (AmazonS3Exception ex)
                {
                    throw new AmazonS3Exception(ex.Message + " " + ex.StackTrace);
                }
                return listOfGrants;
            }
        }

        // For bucket
        public List<S3Grant> ListUsers(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                List<S3Grant> listOfGrants = null;
                try
                {
                    S3AccessControlList acl = client.GetACL(new GetACLRequest
                    {
                        BucketName = bucketName,
                    }).AccessControlList;
                    listOfGrants = acl.Grants;
                }
                catch (AmazonS3Exception ex)
                {
                    throw new AmazonS3Exception(ex.Message + " " + ex.StackTrace);
                }
                return listOfGrants;
            }
        }

        private async Task<bool> DeleteObjects(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                bool isDeleted = false;
                try
                {
                    ListObjectsV2Request request = new ListObjectsV2Request
                    {
                        BucketName = bucketName,
                        MaxKeys = 1000
                    };
                    ListObjectsV2Response response = await client.ListObjectsV2Async(request);
                    if (response.KeyCount != 0)
                    {
                        do
                        {
                            foreach (S3Object obj in response.S3Objects)
                            {
                                GetObjectRequest getObjectRequest = new GetObjectRequest()
                                {
                                    BucketName = bucketName,
                                    Key = obj.Key,
                                };
                                GetObjectResponse getObjectReponse = await client.GetObjectAsync(getObjectRequest);
                                DeleteObjectRequest deleteObjectRequest = new DeleteObjectRequest
                                {
                                    BucketName = bucketName,
                                    Key = obj.Key,
                                    VersionId = getObjectReponse.VersionId
                                };
                                await client.DeleteObjectAsync(deleteObjectRequest);
                            }
                            request.ContinuationToken = response.NextContinuationToken;
                        } while (response.IsTruncated);
                        // call this method again to confirm if all the objects were deleted
                        response = await client.ListObjectsV2Async(request);
                    }
                    if (response.KeyCount == 0)
                    {
                        isDeleted = true;
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    throw new AmazonS3Exception(ex.Message + " " + ex.StackTrace);
                }
                return isDeleted;
            }
        }

        private async Task<bool> IfBucketExist(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                bool found = false;
                try
                {
                    ListBucketsResponse list = await client.ListBucketsAsync();
                    Parallel.ForEach(list.Buckets, (S3Bucket bucket) =>
                    {
                        if (bucket.BucketName == bucketName)
                        {
                            found = true;
                        }
                    });
                }
                catch (AmazonS3Exception e)
                {
                    throw new AmazonS3Exception(e.Message + " " + e.StackTrace);
                }
                return found;

            }
        }

        private void EnableVersioningOnBucket(String bucketName)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                try
                {
                    PutBucketVersioningRequest request = new PutBucketVersioningRequest
                    {
                        BucketName = bucketName,
                        VersioningConfig = new S3BucketVersioningConfig
                        {
                            Status = VersionStatus.Enabled
                        }
                    };

                    client.PutBucketVersioning(request);
                }
                catch (AmazonS3Exception ex)
                {
                    throw new AmazonS3Exception(ex.Message + " " + ex.StackTrace);
                }
            }
        }


        //private async Task UploadChunk(MemoryMappedViewAccessor accessor, String bucketName, FileChunkObject chunk)
        //{
        //    try
        //    {
        //        byte[] bytes = new byte[1024 * 1024 * 1];
        //        int readBytes = accessor.ReadArray(chunk.Offset, bytes, 0, bytes.Length);
        //        Stream uploadStream = new MemoryStream(readBytes);
        //        uploadStream.Write(bytes, 0, readBytes);
        //        ChunkOperation newUpload = new ChunkOperation(chunk, uploadStream, bucketName);
        //        Task<Boolean> uploadTask = newUpload.Upload();
        //        Boolean uploadStatus = await uploadTask;
        //        UpdateChunkProperties(chunk, uploadStatus, readBytes);
        //    }
        //    catch (AmazonS3Exception e)
        //    {
        //        throw new AmazonS3Exception(e.Message);
        //    }
        //}

        //private async Task DownloadChunk(MemoryMappedFile mmf, String bucketName, FileChunkObject chunk)
        //{
        //    try
        //    {
        //        MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(chunk.Offset, chunk.Size);
        //        ChunkOperation toDownload = new ChunkOperation(chunk);
        //        MemoryStream downloadStream = await toDownload.Download(bucketName);
        //        downloadStream.Position = 0;
        //        Byte[] bytes = new Byte[1024 * 1024 * 1];
        //        int readBytes = downloadStream.Read(bytes, 0, bytes.Length);
        //        accessor.WriteArray(0, bytes, 0, readBytes);
        //        chunk.ChunkStatus = true;
        //        chunk.Size = readBytes;
        //        accessor.Dispose();
        //        downloadStream.Dispose();
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //}

        //// For upload
        //private void SetInitialContainerProperties(FileChunkListContainer container, String filePath)
        //{
        //    container.CloudFileName = Path.GetFileName(filePath);
        //    container.status = false;
        //    container.FileSize = 0;
        //    container.ChunkList = new List<FileChunkObject>();
        //}

        //// For upload
        //private void UpdateContainerProperties(FileChunkListContainer container)
        //{
        //    Boolean status = true;
        //    container.FileSize = 0;
        //    foreach (FileChunkObject c in container.ChunkList)
        //    {
        //        container.FileSize += c.Size;
        //    }
        //    foreach (FileChunkObject c in container.ChunkList)
        //    {
        //        if (!c.ChunkStatus)
        //        {
        //            status = false;
        //            break;
        //        }
        //    }
        //    container.status = status;
        //}

        //// For download
        //private void SetInitialContainerProperties(FileChunkListContainer container)
        //{
        //    container.status = false;
        //    container.FileSize = 0;
        //    foreach (FileChunkObject c in container.ChunkList)
        //    {
        //        c.ChunkStatus = false;
        //        c.Size = 0;
        //    }
        //}

        //// For upload
        //private void SetInitialChunkProperties(FileChunkObject chunk)
        //{
        //    chunk.FileName = Guid.NewGuid().ToString();
        //    chunk.ChunkStatus = false;
        //    chunk.Size = 0;
        //    chunk.Offset = 0;
        //}

        //// For upload
        //private void UpdateChunkProperties(FileChunkObject chunk, Boolean status, int readBytes)
        //{
        //    chunk.ChunkStatus = status;
        //    chunk.Size = readBytes;
        //}

        public bool Upload(FileBlock item, MemoryStream stream = null)
        {
            throw new NotImplementedException();
        }

        public void UploadDirectory(DirectoryBlock item)
        {
            throw new NotImplementedException();
        }


        public long GetTotalSpace()
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {

                return -1;
            }
        }

        public long GetUsedSpace()
        {
            long usedSpace = 0;
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                ListBucketsResponse response = client.ListBuckets();
                if (response.Buckets.Count > 0)
                {
                    foreach (S3Bucket bucket in response.Buckets)
                    {
                        ListObjectsV2Request request = new ListObjectsV2Request()
                        {
                            BucketName = bucket.BucketName,
                            MaxKeys = 1000
                        };
                        ListObjectsV2Response response2 = ListObjects(request);
                        if (response2.S3Objects.Count > 0)
                        {
                            do
                            {
                                foreach (S3Object s3Obj in response2.S3Objects)
                                {
                                    usedSpace += s3Obj.Size;
                                }
                                request.ContinuationToken = response2.NextContinuationToken;
                            } while (response2.IsTruncated);
                        }
                    }
                }
            }
            return usedSpace;
        }



        public Task<DirectoryBlock> ListFilesAsync(string directoryId)
        {
            throw new NotImplementedException();
        }


        public void CreateSharedLink()
        {
            using (var client = S3Credential.GetClient())
            {
                GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest()
                {
                    BucketName = "pranjalpradeep",
                    Expires = DateTime.Now.AddMinutes(5)
                };

                string url = client.GetPreSignedURL(request1);
            }

        }

        string ICloudServiceIO.GetRootId()
        {
            return "root";
        }

        public bool DownloadFile(string fileId, string filePath, string bucketName = null, string containerName = null)
        {
            bool status = false;
            try
            {
                using (var client = S3Credential.GetClient())
                {
                    using (var outStream = new FileStream(filePath, FileMode.OpenOrCreate))
                    {
                        Task<GetObjectResponse> task = Task.Run(() => client.GetObjectAsync(bucketName, fileId));
                        var objectResponse = task.Result;
                        var reponseStream = objectResponse.ResponseStream;
                        Task downoadTask = Task.Run(() =>
                        {
                            reponseStream.CopyToAsync(outStream);
                            reponseStream.Flush();
                        });
                        downoadTask.Wait();
                        reponseStream.Dispose();
                        status = true;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return status;
        }

        public bool UploadCloudFile(FileBlock file, string destinationPath = null)
        {
            bool status = false;
            using (var client = S3Credential.GetClient())
            {
                using (var stream = new FileStream(file.path, FileMode.OpenOrCreate))
                {
                    var uploadRequest = new PutObjectRequest()
                    {
                        BucketName = file.container,
                        Key = file.id,
                        InputStream = stream,
                    };
                    Task uploadTask = Task.Run(() => client.PutObjectAsync(uploadRequest));
                    uploadTask.Wait();
                    status = true;
                }
            }
            return status;
        }

        public void DownloadCloudFile(FileBlock file, string localPath)
        {
            using (AmazonS3Client client = S3Credential.GetClient())
            {
                GetObjectRequest request = new GetObjectRequest()
                {
                    BucketName = file.BucketName,
                    Key = file.prefix,
                };
                GetObjectResponse response = client.GetObjectAsync(request).GetAwaiter().GetResult();
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    using (FileStream fileStream = new FileStream(localPath + file.path, FileMode.OpenOrCreate))
                    {
                        Stream stream = response.ResponseStream;
                        stream.CopyTo(fileStream);
                    }
                }
            }
        }

        string ICloudServiceIO.CreateDirectory(string destinationDirectoryId, string newDirectoryName)
        {
            throw new NotImplementedException();
        }

        public void Test()
        {
            using(AmazonS3Client client = S3Credential.GetClient())
            {
                ListObjectsV2Request request = new ListObjectsV2Request()
                {
                    BucketName = "eac3337e34b262ae7f3c0c4cb28a9b1a4f4958f2a3a3dce72fd969a69f4480",
                    Prefix = "documents/",
                    Delimiter = "/"
                };
                ListObjectsV2Response response = client.ListObjectsV2(request);
            }
        }
    }
}
