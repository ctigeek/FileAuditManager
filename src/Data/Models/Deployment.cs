using System;
using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class Deployment
    {
        [BsonId]
        public Guid DeploymentId { get; set; } = Guid.NewGuid();

        public string ApplicationName { get; set; }

        public string ServerName { get; set; }

        public string NetworkPath { get; set; }

        public string Hash { get; set; } = string.Empty;

        public DateTime StartDateTime { get; set; } = DateTime.UtcNow;

        public DateTime EndDateTime { get; set; } = DateTime.MaxValue;

        public Guid MostRecentAudit { get; set; } = Guid.Empty;

        public override string ToString()
        {
            return $"{DeploymentId}.{ApplicationName},{ServerName},{NetworkPath},{Hash},{StartDateTime},{EndDateTime}";
        }
    }
}