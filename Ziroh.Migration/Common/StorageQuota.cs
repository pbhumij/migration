using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Resource

{
    public class StorageQuota
    {
        public long TotalSpace { get; set; }
        public long UsedSpace { get; set; }
    }
}
