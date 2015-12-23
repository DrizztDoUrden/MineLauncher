using System.Collections.Generic;
using System.ServiceModel;

namespace Server.Core
{
    [ServiceContract]
    public interface IUpdateServer
    {
        [OperationContract]
        IDictionary<string, string> RequestFilesList();

        [OperationContract]
        byte[] GetFilePart(string path, int id, out bool isLast);

        [OperationContract]
        string GetHashAlg();
    }
}
