using System.Collections.Generic;

namespace Server.Core
{
    public class UpdateServer : IUpdateServer
    {
        #region IUpdateServer Members

        public string GetHashAlg() => DataContainer.Default.HashAlgorithm;
        public string GetCurrentVersion() => DataContainer.Default.CurrentVersion;

        public IDictionary<string, FileState> RequestDiff(string versionFrom)
            => DataContainer.Default.GetDiff(versionFrom);

        public IDictionary<string, string> RequestCurrentFiles() => DataContainer.Default.CurrentFiles;

        public byte[] GetFilePart(string path, int id, out bool isLast)
            => DataContainer.Default.GetFilePart(path, id, out isLast);

        public void Update() => DataContainer.Default.Update();
        public bool IsArchived(string path) => DataContainer.Default.IsArchived(path);

        #endregion
    }
}