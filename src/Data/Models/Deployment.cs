using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class Deployment
    {
        public const string EmptyHash = "0";
        [BsonId]
        public Guid DeploymentId { get; set; } = Guid.NewGuid();

        public string ApplicationName { get; set; }

        public string ServerName { get; set; }

        public string NetworkPath { get; set; }

        public string Hash { get; set; } = EmptyHash;

        public DateTime StartDateTime { get; set; } = DateTime.UtcNow;

        public DateTime EndDateTime { get; set; } = DateTime.MaxValue;

        public IDictionary<string,string> FileHashes { get; set;  } = new Dictionary<string, string>();

        public Guid MostRecentAudit { get; set; } = Guid.Empty;

        public override string ToString()
        {
            return $"{DeploymentId}.{ApplicationName},{ServerName},{NetworkPath},{Hash},{StartDateTime},{EndDateTime}";
        }
    }
}