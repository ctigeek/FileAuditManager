using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class Application
    {
        [BsonId]
        public string Name { get; set; }

        public bool Enabled { get; set; }
    }
}
