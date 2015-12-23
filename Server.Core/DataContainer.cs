using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Server.Core
{
    public class DataContainer
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        private static extern bool PathRelativePathTo(
            [Out] StringBuilder pszPath,
            [In] string pszFrom,    [In] FileAttributes dwAttrFrom,
            [In] string pszTo,      [In] FileAttributes dwAttrTo
        );

        public string RootPath { get; private set; }
        public string HashAlg { get; private set; }

        public IDictionary<string, string> FileHashes { get; private set; }

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
                        _default = new DataContainer
                        {
                            RootPath = ConfigurationManager.AppSettings["MineLauncher.Server:RootPath"] ?? "/Client/Source/",
                            HashAlg = ConfigurationManager.AppSettings["MineLauncher.Server:HashAlgorithm"] ?? "MD5",
                        };
                }
                return _default;
            }
        }

        #endregion

        #region Constructors

        private DataContainer()
        {
            Update();
        }

        public DataContainer(string rootPath, string hashAlg)
        {
            RootPath = rootPath;
            HashAlg = hashAlg;

            Update();
        }

        #endregion

        #region File hash updating

        public void Update()
        {
            FileHashes = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(RootPath))
                Update(file);
        }

        public void Update(string filePath)
        {
            byte[] hash;

            using (var file = File.OpenRead(filePath))
            using (var hashAlg = HashAlgorithm.Create(HashAlg))
                hash = hashAlg.ComputeHash(file);

            var localPath = new StringBuilder();
            var pathBuilt = PathRelativePathTo(
                localPath,
                RootPath, FileAttributes.Directory,
                filePath, FileAttributes.Normal
            );
            
            filePath = localPath.ToString();
            FileHashes[filePath] = Convert.ToBase64String(hash);
        }

        #endregion
    }
}
