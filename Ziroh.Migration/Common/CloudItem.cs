using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    [DataContract]
    public class CloudItem
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string path { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public String mimeType { get; set; }
        [DataMember]
        public long size { get; set; }
    }
}
