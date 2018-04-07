using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackSpaceClassLibrary
{
    public class Client
    {
        public static CloudFilesProvider GetClient()
        {
        CloudFilesProvider cloudFilesProvider = null;
            try
            {
                CloudIdentity cloudIdentity = new CloudIdentity()
                {
                    APIKey = System.Configuration.ConfigurationManager.AppSettings["RackSpaceApiKey"],
                    Username = System.Configuration.ConfigurationManager.AppSettings["RackSpaceUser"], 
                };
                cloudFilesProvider = new CloudFilesProvider(cloudIdentity);
            }
            catch (Exception)
            {
                throw;
            }
            return cloudFilesProvider;
        }
    }
}
