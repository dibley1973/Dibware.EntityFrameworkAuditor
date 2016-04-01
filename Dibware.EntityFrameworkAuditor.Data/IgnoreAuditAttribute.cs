using System;

namespace Dibware.EntityFrameworkAuditor.Data
{
    /// <summary>
    /// This attribute can be placed on a class or property to prevent the 
    /// auto auditing function including the class or property.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class IgnoreAuditAttribute : Attribute
    {
    }
}