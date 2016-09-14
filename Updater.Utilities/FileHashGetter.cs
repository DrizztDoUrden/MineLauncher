using System;
using System.IO;
using System.Security.Cryptography;

namespace Updater.Utilities
{
    public static class FileHashGetter
    {
        public static string GetHash(string filePath, string hashAlgName)
        {
            var hash = GetFileHash(filePath, hashAlgName);
            return Convert.ToBase64String(hash);
        }

        private static byte[] GetFileHash(string path, string hashAlgName)
        {
            byte[] hash;

            using (var file = File.OpenRead(path))
            {
                using (var hashAlg = HashAlgorithm.Create(hashAlgName))
                {
                    hash = hashAlg.ComputeHash(file);
                }
            }

            return hash;
        }
    }
}