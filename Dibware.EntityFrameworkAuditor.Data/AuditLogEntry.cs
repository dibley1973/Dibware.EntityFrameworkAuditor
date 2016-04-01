
namespace Dibware.EntityFrameworkAuditor.Data
{
    public class AuditLogEntry
    {
        public object Entity { get; set; }
        public AuditLog Log { get; set; }
    }
}
