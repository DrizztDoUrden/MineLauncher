﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Updater.Utilities;

namespace Server.Core
{
    public class HistoryContainer
    {
        public long LastIncrementVersion { get; set; }
        public string CurrentVersion { get; set; }
        [XmlIgnore] public Dictionary<string, Dictionary<string, string>> History { get; private set; }
        [XmlIgnore] public Dictionary<string, string> CurrentVersionFiles => History[CurrentVersion];

        public HistoryContainer()
        {
            History = new Dictionary<string, Dictionary<string, string>>();
        }

        #region Xml serialization

        [XmlElement(ElementName = "History")]
        public List<Pair<string, List<Pair<string, string>>>> HistorySerializer
        {
            get
            {
                if (!History.Any()) return null;
                
                return History
                    .Select(kvp =>
                        new Pair<string, List<Pair<string, string>>>(
                            kvp.Key,
                            kvp.Value
                                .Select(Pair<string, string>.FromKvp)
                                .ToList()
                        )
                    )
                    .ToList();
            }
            set
            {
                History = value.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToDictionary(
                        fileList => fileList.Key,
                        fileList => fileList.Value
                    )
                );
            }
        }

        #endregion

        public string AddVersion(Dictionary<string, string> files, string version = null)
        {
            if (CurrentVersion != null && !GetDiff(CurrentVersionFiles, files).Any()) return null;

            if (version == null)
                version = (++LastIncrementVersion).ToString();

            History.Add(version, files);
            CurrentVersion = version;
            return version;
        }

        public Dictionary<string, FileState> GetDiff(Dictionary<string, string> from, Dictionary<string, string> to)
        {
            var filesFrom = new Dictionary<string, string>(from);
            var diff = new Dictionary<string, FileState>();

            foreach (var file in to)
            {
                var path = file.Key;

                if (filesFrom.ContainsKey(path))
                {
                    if (file.Value != filesFrom[path])
                        diff.Add(path, file.Value);

                    filesFrom.Remove(path);
                }
                else
                {
                    diff.Add(path, file.Value);
                }
            }

            foreach (var file in filesFrom)
                diff.Add(file.Key, new FileState { IsRemoved = true });

            return diff;
        }

        public Dictionary<string, FileState> GetDiff(string versionFrom, string version = null)
        {
            if (string.IsNullOrEmpty(version))
                version = CurrentVersion;

            if (!History.ContainsKey(version))
                return CurrentVersionFiles.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (FileState)kvp.Value
                );

            var files = History[version];
            var filesFrom = new Dictionary<string, string>(History[versionFrom]);
            
            return GetDiff(filesFrom, files);
        }
    }
}