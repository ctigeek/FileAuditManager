using System.Collections.Generic;

namespace FileAuditManager.Controllers.Models
{
    public class ModifiedApplication
    {
        public bool? Enabled { get; set; }

        public bool? HashHiddenFiles { get; set; }

        public IList<string> FileExclusionExpressions { get; set; }

    }
}
