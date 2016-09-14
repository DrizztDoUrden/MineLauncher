using System.Runtime.Serialization;

namespace Server.Core
{
    [DataContract]
    public class FileState
    {
        [DataMember] public string Hash { get; set; }
        [DataMember] public bool IsRemoved { get; set; }

        public static implicit operator FileState(string hash)
            => new FileState
            {
                Hash = hash,
                IsRemoved = false
            };
    }
}