using Amazon.Runtime;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.CloudService.AmazonS3
{
    static class S3Credential
    {
        private static readonly String accessKey = System.Configuration.ConfigurationManager.AppSettings["accessKey"];
        private static readonly String secretKey = System.Configuration.ConfigurationManager.AppSettings["secretKey"];

        public static AmazonS3Client GetClient()
        {
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = "https://s3.amazonaws.com",
                SignatureVersion = "s3",
            };
            AmazonS3Client client = new AmazonS3Client(accessKey, secretKey, config);
            return client;
        }
    }
}
