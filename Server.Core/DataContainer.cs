using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Updater.Utilities;

namespace Server.Core
{
    public class DataContainer
    {
        private HistoryContainer _history;

        public string RootPath { get; private set; }
        public string HashAlg { get; private set; }
        public int BufferSize { get; private set; }
        
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathRelativePathTo(
            [Out] StringBuilder pszPath,
            [In] string pszFrom, [In] FileAttributes dwAttrFrom,
            [In] string pszTo, [In] FileAttributes dwAttrTo
        );

        private static string ExeDir => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static object _histFileLock = new object();

        #region Singleton

        private static object _defaultLock = new object();
        private static DataContainer _default;

        public static DataContainer Default
        {
            get
            {
                lock (_defaultLock)
                {
                    if (_default == null)
                    {
                        int buffSize;
                        bool updateRequired;

                        bool.TryParse(ConfigurationManager.AppSettings["MineLauncher.Server:UpdateEveryStart"], out updateRequired);
                        if (!int.TryParse(ConfigurationManager.AppSettings["MineLauncher.Server:BufferSize"], out buffSize)) buffSize = 32 * 1024;

                        var histPath = ConfigurationManager.AppSettings["MineLauncher.Server:HistoryPath"];
                        if (!histPath.Contains(":"))
                            histPath = $"{ExeDir}\\{histPath}";

                        _default = new DataContainer(
                            rootPath: ConfigurationManager.AppSettings["MineLauncher.Server:RootPath"],
                            hashAlg: ConfigurationManager.AppSettings["MineLauncher.Server:HashAlgorithm"],
                            bufferSize: buffSize,
                            history: LoadHistory(histPath, ref updateRequired)
                        );

                        if (!_default.RootPath.Contains(":"))
                            _default.RootPath = $"{ExeDir}\\{_default.RootPath}";

                        if (updateRequired)
                            _default.Update();
                    }
                }
                return _default;
            }
        }

        #endregion

        #region Constructors

        public DataContainer(string rootPath, string hashAlg, int bufferSize, HistoryContainer history)
        {
            RootPath = rootPath;
            HashAlg = hashAlg;
            BufferSize = bufferSize;
            _history = history;
        }

        #endregion

        #region File hash updating

        private static string GetRelativePath(string file, string root)
        {
            var localPath = new StringBuilder();
            var pathBuilt = PathRelativePathTo(
                localPath,
                root, FileAttributes.Directory,
                file, FileAttributes.Normal
            );

            if (!pathBuilt)
                return null;

            return localPath.ToString();
        }

        public void Update()
        {
            var fileHashes = new Dictionary<string, string>();

            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);
            
            foreach (var file in Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories))
            {
                var path = GetRelativePath(file, RootPath);
                if (string.IsNullOrEmpty(path)) continue;
                fileHashes[path] = FileHashGetter.GetHash(file, HashAlg);
            }

            var curVer = _history.CurrentVersion;
            var newVer = _history.AddVersion(fileHashes);
            if (newVer != curVer) SaveHistory();
        }

        #endregion

        public static HistoryContainer LoadHistory(string histPath, ref bool updateRequired)
        {
            if (File.Exists(histPath))
            {
                var json = JsonSerializer.CreateDefault();

                lock (_histFileLock)
                {
                    using (var file = File.OpenRead(histPath))
                    using (var reader = new StreamReader(file))
                        return (HistoryContainer)json.Deserialize(reader, typeof(HistoryContainer));
                }
            }
            else
            {
                updateRequired = true;
                return new HistoryContainer();
            }
        }

        public void SaveHistory()
        {
            var histPath = ConfigurationManager.AppSettings["MineLauncher.Server:HistoryPath"];
            if (!histPath.Contains(":"))
                histPath = $"{ExeDir}\\{histPath}";

            var histDir = Path.GetDirectoryName(histPath);
            if (!Directory.Exists(histDir)) Directory.CreateDirectory(histDir);
            if (File.Exists(histPath)) File.Delete(histPath);

            var json = JsonSerializer.CreateDefault();
            json.Formatting = Formatting.Indented;
            lock (_histFileLock)
            {
                using (var file = File.OpenWrite(histPath))
                using (var writer = new StreamWriter(file))
                    json.Serialize(writer, _history, typeof(HistoryContainer));
            }
        }

        public Dictionary<string, FileState> GetDiff(string versionFrom, string version = null) => _history.GetDiff(versionFrom, version);
        public Dictionary<string, string> CurrentFiles => _history.CurrentVersionFiles;
        public string CurrentVersion => _history.CurrentVersion;

        public byte[] GetFilePart(string path, int id, out bool isArchived, out bool isLast)
        {
#warning Добавить реальную архивацию
            isArchived = false;

            if (!_history.CurrentVersionFiles.ContainsKey(path))
            {
                isArchived = false;
                isLast = true;
                return null;
            }

            var fullPath = $"{RootPath}\\{path}";
            byte[] buffer = new byte[BufferSize];

            using (var file = File.OpenRead(fullPath))
            {
                var offset = id * BufferSize;
                file.Position = offset;
                var length = file.Read(buffer, 0, BufferSize);

                isLast = file.Length == offset + length;
            }

            return buffer;
        }
    }
}
