using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Resource
{
    [DataContract]
    [KnownType(typeof(DirectoryBlock))]
    public class FileBlock
    {   
        [DataMember]
        public String id { get; set; }
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String Description { get; set; }
        [DataMember]
        public long? Size { get; set; }
        [DataMember]
        public String SharedLink { get; set; }
        [DataMember]
        public List<Owner> Owners;
        [DataMember]
        public String CreationTime { get; set; }
        [DataMember]
        public List<User> Users { get; set; }
        // specifically for azure blob
        [DataMember]
        public string container { get; set; }
        [DataMember]
        public string path { get; set; }
        [DataMember]
        public string mimeType { get; set; }
        [DataMember]
        public List<string> Parent { get; set; }
        [DataMember]
        public string BucketName { get; set; }
        [DataMember]
        public Uri CloudUri { get; set; }
        [DataMember]
        public string prefix { get; set; }
        [DataMember]
        public bool DownloadStatus { get; set; }
        [DataMember]
        public bool UploadStatus { get; set; }
    }
    
    [DataContract]
    public class DirectoryBlock : FileBlock
    {
        [DataMember]
        public List<FileBlock> Files { get; set; }
        [DataMember]
        public List<DirectoryBlock> SubDirectories { get; set; }
        // specifically for azure blob
        [DataMember]
        public string type { get; set; }
        // specifically for azure blob
        [DataMember]
        public string accessType { get; set; } 
    }
}
