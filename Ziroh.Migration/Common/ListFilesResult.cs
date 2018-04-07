using Migration.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    public class ListFilesResult
    {
        public List<FileBlock> Files = new List<FileBlock>();
        public bool status = false;
    }
}
