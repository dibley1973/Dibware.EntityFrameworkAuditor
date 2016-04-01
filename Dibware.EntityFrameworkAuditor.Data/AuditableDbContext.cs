using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace Dibware.EntityFrameworkAuditor.Data
{
    /// <summary>
    /// Extends the EF DbContext to provide automatic audit
    /// </summary>
    /// <remarks>
    /// Based upon concept by Shawn Weisfeld.
    /// Ref: http://www.drowningintechnicaldebt.com/ShawnWeisfeld/archive/2011/07/26/auditing-with-entity-framework.aspx
    /// </remarks>
    public class AuditableDbContext : DbContext
    {
        private readonly string _username;

        public AuditableDbContext(string connectionString)
            : this(connectionString, "Unknown user")
        { }

        public AuditableDbContext(string connectionString, string username)
            : base(connectionString)
        {
            _username = username;
        }

        public bool IgnoreAuditLogExceptions
        {
            get { return Properties.Settings.Default.IgnoreAuditLogExceptions; }
        }
        
        public bool UseAuditLogging
        {
            get { return Properties.Settings.Default.UseAuditLogging; }
        }

        public DbSet<AuditLog> AuditLog { get; set; }

        public override int SaveChanges()
        {
            if (!UseAuditLogging)
            {
                return base.SaveChanges();
            }

            var logs = new List<AuditLogEntry>();

            try
            {
                AddEntriesToLogs(logs);
            }
            catch (Exception)
            {
                if (!IgnoreAuditLogExceptions)
                {
                    throw;
                }
                // Otherwise do nothing
            }

            // Note: The Id for entities that are added may not be populated until after the first "save changes"
            int entityChangeCount = base.SaveChanges();

            try
            {
                AddLogsToDbSet(logs);
                base.SaveChanges(); // Save Audit log changes
            }
            catch (Exception)
            {
                if (!IgnoreAuditLogExceptions)
                {
                    throw;
                }
                // Otherwise do nothing
            }

            return entityChangeCount; // As we don't want to include audit entries
        }

        private void AddLogsToDbSet(List<AuditLogEntry> logs)
        {
            foreach (AuditLogEntry auditLog in logs)
            {
                var entityHasChanged = auditLog.Log.OldValue != auditLog.Log.NewValue;
                if (entityHasChanged)
                {
                    UpdateAuditLogKeyMembers(auditLog);

                    AuditLog.Add(auditLog.Log);
                }
            }
        }

        private void AddEntriesToLogs(List<AuditLogEntry> logs)
        {
            var timestamp = DateTime.UtcNow;
            var batchId = Guid.NewGuid();

            foreach (DbEntityEntry entityEntry in ChangeTracker.Entries())
            {
                var factory = new AuditLogFactory(entityEntry, _username, timestamp, batchId);
                logs.AddRange(factory.GetEntries());
            }
        }

        private static string GetFormattedValue(AuditLogEntry auditLog, PropertyInfo keyProperty)
        {
            var value = keyProperty.GetValue(auditLog.Entity);
            var formattedValue = value is string || value is DateTime
                ? string.Format(@"""{0}""", value)
                : value.ToString();

            return formattedValue;
        }

        protected IEnumerable<string> GetKeyPropertyNames(Type type)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;

            return GetKeyPropertyNames(type, objectContext.MetadataWorkspace);
        }

        private static IEnumerable<string> GetKeyPropertyNames(Type type, MetadataWorkspace workspace)
        {
            if (type == null) throw new ArgumentNullException("type");

            bool typeIsProxy = type.Assembly.FullName.StartsWith("EntityFrameworkDynamicProxies");
            if (typeIsProxy) type = type.BaseType;

            EdmType edmType;
            const string keyMemberMetaDataName = "KeyMembers";

            if (workspace.TryGetType(type.Name, type.Namespace, DataSpace.OSpace, out edmType))
            {
                return edmType.MetadataProperties
                    .Where(mp => mp.Name == keyMemberMetaDataName)
                    .SelectMany(mp => mp.Value as ReadOnlyMetadataCollection<EdmMember>)
                    .OfType<EdmProperty>()
                    .Select(edmProperty => edmProperty.Name);
            }

            return null;
        }

        private void UpdateAuditLogKeyMembers(AuditLogEntry auditLog)
        {
            var entityType = auditLog.Entity.GetType();
            var keyColumnNames = GetKeyPropertyNames(entityType).ToList();
            var keyColumnValues = new List<string>();

            foreach (string keyColumnName in keyColumnNames)
            {
                PropertyInfo keyProperty = entityType.GetProperties().SingleOrDefault(property => property.Name == keyColumnName);
                if (keyProperty == null) continue;

                string formattedValue = GetFormattedValue(auditLog, keyProperty);
                keyColumnValues.Add(formattedValue);
            }

            var entityPrimaryKey = string.Join(",", keyColumnNames);
            auditLog.Log.KeyMembers = entityPrimaryKey;

            string entityPrimaryKeyValues = string.Join(",", keyColumnValues);
            auditLog.Log.KeyValues = entityPrimaryKeyValues;
        }
    }
}