using System.Collections.Generic;
using System.ServiceModel;

namespace Server.Core
{
    [ServiceContract]
    public interface IUpdateServer
    {
        [OperationContract] IDictionary<string, FileState> RequestDiff(string version);
        [OperationContract] IDictionary<string, string> RequestCurrentFiles();
        [OperationContract] byte[] GetFilePart(string path, int id, out bool isArchived, out bool isLast);
        [OperationContract] string GetHashAlg();
        [OperationContract] string GetCurrentVersion();
    }
}
