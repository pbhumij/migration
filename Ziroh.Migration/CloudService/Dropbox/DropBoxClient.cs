using Dropbox.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.CloudService.DropboxService
{
    public class DropBoxClient
    {
        private static readonly String AccessToken = System.Configuration.ConfigurationManager.AppSettings["DBXToken"];
       
        public static DropboxClient GetClient()
        {
            DropboxClient client = null;
            try
            {
               client = new DropboxClient(AccessToken);
            }
            catch(DropboxException e)
            {
                throw new Exception(e.Message + " " + e.StackTrace);
            }
            return client;
        }
    }
}
