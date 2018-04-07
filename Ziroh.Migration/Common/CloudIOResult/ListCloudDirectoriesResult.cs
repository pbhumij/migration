using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    public class ListCloudDirectoriesResult
    {
        private List<DirectoryBlock> _directories = null;

        public List<DirectoryBlock> Directories
        {
            get
            {
                return  _directories;
            }
            set
            {
                _directories = value;
            }
        }
    }
}
