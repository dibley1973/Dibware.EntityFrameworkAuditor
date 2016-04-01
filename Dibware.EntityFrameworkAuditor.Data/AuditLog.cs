using System;

namespace Dibware.EntityFrameworkAuditor.Data
{
    public class AuditLog
    {
        public int Id { get; set; }
        public Guid BatchId { get; set; }
        public string ObjectType { get; set; }
        public string KeyMembers { get; set; }
        public string KeyValues { get; set; }
        public string Action { get; set; }
        public string Property { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Username { get; set; }
        public DateTime UtcDate { get; set; }
    }
}
