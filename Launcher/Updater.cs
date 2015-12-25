﻿using System.IO;
using System.Threading.Tasks;
using Launcher.UpdateServer;
using Updater.Utilities;

namespace Launcher
{
    public class Updater
    {
        private async Task DownloadFile(UpdateServerClient server, string filePath, string fileHash, string path, string hashAlg)
        {
            var fileFullPath = Path.Combine(path, filePath);

            if (File.Exists(fileFullPath))
            {
                var hash = FileHashGetter.GetHash(fileFullPath, hashAlg);
                if (fileHash == hash)
                    return;

                File.Delete(fileFullPath);
            }

            var dir = Path.GetDirectoryName(fileFullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var file = File.Create(fileFullPath))
            {
                var isLast = false;
                var id = 0;

                while (!isLast)
                {
                    var responce = await server.GetFilePartAsync(new GetFilePartRequest(filePath, id++));
#warning Добавить реальную разархивацию
                    await file.WriteAsync(responce.GetFilePartResult, 0, responce.GetFilePartResult.Length);
                    isLast = responce.isLast;
                }
            }
        }

        private async Task UpdateFiles(UpdateServerClient server, string path, string hashAlg, string version)
        {
            var files = await server.RequestDiffAsync(version);

            foreach (var fileInfo in files)
            {
                if (fileInfo.Value.IsRemoved)
                {
                    var fileFullPath = Path.Combine(path, fileInfo.Key);

                    if (File.Exists(fileFullPath))
                        File.Delete(fileFullPath);

                    continue;
                }

                await DownloadFile(server, fileInfo.Key, fileInfo.Value.Hash, path, hashAlg);
            }
        }

        private async Task DownloadFiles(UpdateServerClient server, string path, string hashAlg)
        {
            var files = await server.RequestCurrentFilesAsync();

            foreach (var fileInfo in files)
                await DownloadFile(server, fileInfo.Key, fileInfo.Value, path, hashAlg);
        }

        public async Task<string> Update(string path, bool reload, bool _validate, string version)
        {
            using (var server = new UpdateServerClient())
            {
                var hashAlg = await server.GetHashAlgAsync();
                var servVersion = await server.GetCurrentVersionAsync();

                if (reload || string.IsNullOrEmpty(version))
                {
                    await DownloadFiles(server, path, hashAlg);
                    return servVersion;
                }

                if (servVersion == version && !_validate) return version;

                await UpdateFiles(server, path, hashAlg, version);
                return servVersion;
            }
        }
    }
}