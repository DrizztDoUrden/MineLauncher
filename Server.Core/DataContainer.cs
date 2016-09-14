using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Updater.Utilities;

namespace Server.Core
{
    public class DataContainer
    {
        public string RootPath { get; }
        public string TempPath { get; }
        public IEnumerable<string> ArchiveBlackList { get; }

        public string HashAlgorithm { get; }
        public int BufferSize { get; }
        public bool ArchiveFiles { get; }

        #region Constructors

        public DataContainer(string rootPath,
                             string tempPath,
                             string hashAlg,
                             int bufferSize,
                             bool archiveFiles,
                             IEnumerable<string> archiveBlackList,
                             HistoryContainer history)
        {
            TempPath = tempPath;
            RootPath = rootPath;
            HashAlgorithm = hashAlg;
            BufferSize = bufferSize;
            ArchiveFiles = archiveFiles;
            ArchiveBlackList = archiveBlackList;
            _history = history;
        }

        #endregion

        private static readonly object _histFileLock = new object();
        private readonly HistoryContainer _history;

        private static string ExeDir => Path.GetDirectoryName(
            Assembly.GetExecutingAssembly()
                .Location);

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)] private static extern bool PathRelativePathTo(
            [Out] StringBuilder pszPath,
            [In] string pszFrom,
            [In] FileAttributes dwAttrFrom,
            [In] string pszTo,
            [In] FileAttributes dwAttrTo
        );

        #region Config helpers

        private static string GetCfgValue(string name)
            => ConfigurationManager.AppSettings[$"MineLauncher.Server:{name}"];

        private static T GetCfgValue<T>(string name, TryParse<T> parser)
        {
            T value;
            if (!parser(GetCfgValue(name), out value))
                return default(T);
            return value;
        }

        private static T GetCfgValue<T>(string name, TryParse<T> parser, T defaultValue)
        {
            T value;
            if (!parser(GetCfgValue(name), out value))
                return defaultValue;
            return value;
        }

        private static string FixPath(string name)
        {
            var path = GetCfgValue(name);
            if (!path.Contains(":"))
                path = $"{ExeDir}\\{path}";

            return path;
        }

        #endregion

        #region Cfg values

        private static bool UpdateEveryStart => GetCfgValue<bool>("UpdateEveryStart", bool.TryParse);
        private static int CfgBufferSize => GetCfgValue("BufferSize", int.TryParse, 32*1024);
        private static string CfgHashAlgorithm => GetCfgValue("HashAlgorithm");

        private static string HistoryPath => FixPath("HistoryPath");
        private static string CfgRootPath => FixPath("RootPath");
        private static string CfgTempPath => FixPath("TempPath");

        private static bool CfgArchiveFiles => GetCfgValue<bool>("ArchiveFiles", bool.TryParse);

        private static IEnumerable<string> CfgArchiveBlackList => GetCfgValue("ArchiveBlackList")
            .Split(',')
            .Select(s => s.Trim());

        #endregion

        #region Singleton

        private static readonly object _defaultLock = new object();
        private static DataContainer _default;

        public static DataContainer Default
        {
            get
            {
                lock (_defaultLock)
                {
                    if (_default == null)
                    {
                        var updateRequired = UpdateEveryStart;

                        _default = new DataContainer(
                            CfgRootPath,
                            CfgTempPath,
                            CfgHashAlgorithm,
                            CfgBufferSize,
                            CfgArchiveFiles,
                            CfgArchiveBlackList,
                            LoadHistory(HistoryPath, ref updateRequired)
                        );

                        if (updateRequired)
                            _default.Update();
                    }
                }
                return _default;
            }
        }

        #endregion

        #region File hash updating

        private static string GetRelativePath(string file, string root)
        {
            var localPath = new StringBuilder();
            var pathBuilt = PathRelativePathTo(
                localPath,
                root,
                FileAttributes.Directory,
                file,
                FileAttributes.Normal
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
                fileHashes[path] = FileHashGetter.GetHash(file, HashAlgorithm);
            }

            var curVer = _history.CurrentVersion;
            var newVer = _history.AddVersion(fileHashes);

            if (newVer != curVer)
            {
                SaveHistory();
                if (ArchiveFiles)
                    PrepareArchive();
            }
        }

        #endregion

        #region History save/load

        public static HistoryContainer LoadHistory(string histPath, ref bool updateRequired)
        {
            if (File.Exists(histPath))
            {
                var json = JsonSerializer.CreateDefault();

                lock (_histFileLock)
                {
                    using (var file = File.OpenRead(histPath))
                    {
                        using (var reader = new StreamReader(file))
                        {
                            return (HistoryContainer) json.Deserialize(reader, typeof(HistoryContainer));
                        }
                    }
                }
            }
            updateRequired = true;
            return new HistoryContainer();
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
                {
                    using (var writer = new StreamWriter(file))
                    {
                        json.Serialize(writer, _history, typeof(HistoryContainer));
                    }
                }
            }
        }

        #endregion

        #region Archiving

        private string GetFilePath(string path)
        {
            var rawPath = $"{RootPath}\\{path}";
            if (!ArchiveFiles) return rawPath;

            var fullPath = $"{TempPath}\\{path}";
            if (File.Exists(fullPath)) return fullPath;

            return rawPath;
        }

        private void PrepareArchive()
        {
            if (Directory.Exists(TempPath)) Directory.Delete(TempPath, true);

            foreach (var path in Directory.GetFiles(RootPath, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(path);
                if (ArchiveBlackList.Contains(ext)) continue;

                var fileDir = Path.GetDirectoryName(path);
                if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

                var archivePath = $"{TempPath}\\{GetRelativePath(path, RootPath)}";
                var archiveDir = Path.GetDirectoryName(archivePath);
                if (!Directory.Exists(archiveDir)) Directory.CreateDirectory(archiveDir);

                using (var file = File.OpenRead(path))
                {
                    using (var archiveFile = File.OpenWrite(archivePath))
                    {
                        using (var archive = new GZipStream(archiveFile, CompressionMode.Compress, false))
                        {
                            file.CopyTo(archive);
                        }
                    }
                }
            }
        }

        #endregion

        #region API

        public Dictionary<string, FileState> GetDiff(string versionFrom) => _history.GetDiff(versionFrom);
        public Dictionary<string, string> CurrentFiles => _history.CurrentVersionFiles;
        public string CurrentVersion => _history.CurrentVersion;
        public bool IsArchived(string path) => File.Exists($"{TempPath}\\{path}");

        public byte[] GetFilePart(string path, int id, out bool isLast)
        {
            if (!_history.CurrentVersionFiles.ContainsKey(path))
            {
                isLast = true;
                return null;
            }

            var buffer = new byte[BufferSize];
            using (var file = File.OpenRead(GetFilePath(path)))
            {
                var offset = id*BufferSize;
                file.Position = offset;
                var length = file.Read(buffer, 0, BufferSize);

                isLast = file.Length == offset + length;
            }

            return buffer;
        }

        #endregion
    }
}