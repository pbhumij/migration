using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    [DataContract]
    public class CloudStructure
    {
        private DirectoryBlock _RootDirectory = null;

        [DataMember]
        public DirectoryBlock RootDirectory
        {
            get
            {
                return _RootDirectory;
            }
            set
            {
                _RootDirectory = value;
            }
        }
    }
}
