using System.Collections.Generic;

namespace Server.Core
{
    public class UpdateServer: IUpdateServer
    {
        public IDictionary<string, string> RequestFilesList() => DataContainer.Default.FileHashes;
        public string GetHashAlg() => DataContainer.Default.HashAlg;

        public byte[] GetFilePart(string path, int id, out bool isLast)
        {
            isLast = true;
            return null;
        }
    }
}
