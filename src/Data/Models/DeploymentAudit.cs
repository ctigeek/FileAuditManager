using System;
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
    }
}