using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MongoDB.Bson.Serialization.Attributes;

namespace FileAuditManager.Data.Models
{
    public class Application
    {
        [BsonId]
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public IList<string> FileExclusionExpressions { get; set; } = new List<string>();

        public bool HashHiddenFiles { get; set; } = false;

        public IList<Regex> GetRegularExpressions()
        {
            return FileExclusionExpressions.Select(f => new Regex(f)).ToList();
        }
    }
}
