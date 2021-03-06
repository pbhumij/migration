﻿using Migration.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    public interface ICloudServiceIO
    {
        void GetFiles(DirectoryBlock parentDirectory);
        string GetRootId();
        string CreateDirectory(string destinationDirectoryId, string newDirectoryName);
        void DownloadCloudFile(FileBlock file, string localPath);
        bool UploadCloudFile(FileBlock file, string destinationPath = null);
        long GetTotalSpace();
        long GetUsedSpace();
    }
}
