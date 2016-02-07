using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class DeploymentAudit
    {
        [BsonId]
        public Guid DeploymentAuditId { get; set; } = Guid.NewGuid();

        public Guid DeploymentId { get; set; }

        public DateTime AuditDateTime { get; set; } = DateTime.UtcNow;

        public string Hash { get; set; }

        public bool ValidHash { get; set; }

        public IList<FileHashMismatch> FileHashMismatches { get; set; } = new List<FileHashMismatch>();
    }

    public class FileHashMismatch
    {
        public string FilePath { get; set; }
        public string OriginalHash { get; set; }
        public string AuditHash { get; set; }
    }
}