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

        public string Hash { get; set; } = Deployment.EmptyHash;

        public bool ValidHash { get; set; } = false;

        public string Error { get; set; } = string.Empty;

        public IList<FileHashMismatch> FileHashMismatches { get; set; } = new List<FileHashMismatch>();
    }

    public class FileHashMismatch
    {
        public FileHash OriginalHash { get; set; }
        public FileHash AuditHash { get; set; }
    }
}