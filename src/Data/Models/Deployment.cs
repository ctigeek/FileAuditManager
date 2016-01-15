using System;
using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class Deployment
    {
        [BsonId]
        public Guid? DeploymentId { get; set; }

        public string ApplicationName { get; set; }

        public string ServerName { get; set; }

        public string NetworkPath { get; set; }

        public string Hash { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public override string ToString()
        {
            return $"{DeploymentId}.{ApplicationName},{ServerName},{NetworkPath},{Hash},{StartDateTime},{EndDateTime}";
        }
    }

    public class DeploymentAudit
    {
        [BsonId]
        public Guid DeploymentAuditId { get; set; } = Guid.NewGuid();

        public Guid DeploymentId { get; set; }

        public Guid DeploymentHashId { get; set; }

        public DateTime AuditDateTime { get; set; }

        public string Hash { get; set; }

        public bool ValidHash { get; set; }
    }
}