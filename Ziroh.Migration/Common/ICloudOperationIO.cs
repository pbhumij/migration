using Migration.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    /// <summary
    /// A base class for cloud specific functions.
    /// </summary>
    public interface ICloudOperationIO
    {
        CloudStructure GetCloudRootStructure();
        MigrationResult MigrateToOstor(DirectoryBlock directoryBlock, ICloudServiceIO cloudService);
        DownloadCloudDirectory DownloadCloudDirectory(string cloudDirectoryId, string localPath);
        FileDownloadResult DownloadCloudFile(string cloudFileId, string localPath, FileBlock file = null);
        DirectoryUploadResult UploadCloudDirectory(string localDirectoryPath, string destinationDirectoryId);
        CloudCreateDirectoryResult CreateDirectory(string directoryName = null, string destinationDirectoryId = null, string relativePath = null);
        FileUploadResult UploadCloudFile(string filePath, string destinationDirectoryId);
        StorageQuota GetStorageQuota();
        string GetRootId();
    }
}
