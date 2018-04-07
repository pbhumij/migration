﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    public interface IStorageQuota
    {
        long GetTotalSpace();
        long GetUsedSpace();
    }
}
