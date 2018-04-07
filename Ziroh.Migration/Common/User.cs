using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Resource
{
    public class User
    {
        public String DisplayName { get; set; }
        public String EmailAddress { get; set; }
        public List<Permission> Permissions { get; set; }
    }
}
