using Migration.Common;
using Migration.Resource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Ziroh.Migration.MigrationIO
{
    public class TransactionFile : MigrationResource
    {
        Object thisLock = new object();
        public void Create(DirectoryBlock directoryBlock)
        {
            try
            {
                DataContractJsonSerializer serialize = new DataContractJsonSerializer(typeof(DirectoryBlock));
                lock (thisLock)
                {
                    using (FileStream stream = new FileStream(TransactionFilePath, FileMode.Create))
                    {
                        serialize.WriteObject(stream, directoryBlock);
                        stream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public DirectoryBlock Deserialize()
        {
            DirectoryBlock directory = new DirectoryBlock();
            try
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DirectoryBlock));
                lock (new object())
                {
                    using (FileStream stream = new FileStream(TransactionFilePath, FileMode.Open))
                    {
                        stream.Position = 0;
                        directory = (DirectoryBlock)ser.ReadObject(stream);
                    }
                }
                return directory;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
      
    }
}
