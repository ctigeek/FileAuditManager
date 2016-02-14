using System;

namespace FileAuditManager.Data.Models
{
    public class FileHash
    {
        public string Path { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool IsHidden { get; set; }
        public string Hash { get; set; }

        public override string ToString()
        {
            return $"{Path} : {Hash}";
        }
    }
}
