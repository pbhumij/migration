using Box.V2.Config;
using Box.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Box.V2.Exceptions;
using Box.V2.Auth;

namespace Migration.CloudService.Azure
{
    static class BoxClient
    {
        private static readonly String ClientId = System.Configuration.ConfigurationManager.AppSettings["BoxClientId"];
        private static readonly String ClientSecret = System.Configuration.ConfigurationManager.AppSettings["BoxClientSecret"];
        private static readonly String AccessToken = System.Configuration.ConfigurationManager.AppSettings["BoxAccessToken"];
        private static readonly Uri redirectUri = new Uri("https://app.box.com");

        public static Box.V2.BoxClient GetClient()
        {
            Box.V2.BoxClient client = null;
            try
            {
                var config = new BoxConfig(ClientId, ClientSecret, redirectUri);
                var session = new OAuthSession(AccessToken, "REFRESH_TOKEN", 3600, "bearer");
                client = new Box.V2.BoxClient(config, session);
            }
            catch (BoxException e)
            {
                Console.WriteLine(e.Message);
            }
            return client;
        }
    }
}

