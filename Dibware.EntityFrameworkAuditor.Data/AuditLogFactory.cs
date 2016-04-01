using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Dibware.EntityFrameworkAuditor.Data
{
    public class AuditLogFactory
    {
        private readonly DbEntityEntry _entityEntry;
        private readonly EntityState _entityState;
        private readonly string _action;
        private readonly string _username;
        private readonly DateTime _timestamp;
        private readonly Guid _batchId;
        private readonly Type _entryType;
        private readonly List<string> _propertiesToLog;
        private readonly List<AuditLogEntry> _auditLogEntries;

        public AuditLogFactory(DbEntityEntry entityEntry, string username, DateTime timestamp, Guid batchId)
        {
            if (entityEntry == null) throw new ArgumentNullException("entityEntry");
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            if (batchId == Guid.Empty) throw new ArgumentException(@"batchId must be set", "batchId");

            _entityEntry = entityEntry;
            _username = username;
            _timestamp = timestamp;
            _batchId = batchId;

            _entityState = _entityEntry.State;
            _action = _entityEntry.State.ToString();
            _propertiesToLog = _entityState == EntityState.Deleted 
                ? _entityEntry.OriginalValues.PropertyNames.ToList()  
                : _entityEntry.CurrentValues.PropertyNames.ToList();
            _entryType = _entityEntry.Entity.GetType();

            _auditLogEntries = new List<AuditLogEntry>();
        }

        public List<AuditLogEntry> GetEntries()
        {
            CreateEntries();
            return _auditLogEntries;
        }

        private void CreateEntries()
        {
            ClearAuditLogEntries();

            foreach (var propertyName in _propertiesToLog)
            {
                string oldValue = GetOldValue(propertyName);
                string newValue = GetNewValue(propertyName);

                var auditLogEntry = new AuditLogEntry
                {
                    Entity = _entityEntry.Entity,
                    Log = new AuditLog
                    {
                        ObjectType = _entryType.Name,
                        BatchId = _batchId,
                        Action = _action,
                        Property = propertyName,
                        OldValue = oldValue,
                        NewValue = newValue,
                        Username = _username,
                        UtcDate = _timestamp
                    }
                };

                _auditLogEntries.Add(auditLogEntry);
            }
        }

        private string GetNewValue(string propertyName)
        {
            string newValue;

            switch (_entityState)
            {
                case EntityState.Added:
                case EntityState.Modified:
                case EntityState.Unchanged:
                    newValue = GetFormattedValue(_entityEntry.CurrentValues[propertyName]);
                    break;

                default:
                    newValue = null;
                    break;
            }

            return newValue;
        }

        private string GetOldValue(string propertyName)
        {
            string oldValue;

            switch (_entityState)
            {
                case EntityState.Deleted:
                case EntityState.Modified:
                case EntityState.Unchanged:
                    oldValue = GetFormattedValue(_entityEntry.OriginalValues[propertyName]);
                    break;

                default:
                    oldValue = null;
                    break;
            }

            return oldValue;
        }

        private void ClearAuditLogEntries()
        {
            _auditLogEntries.Clear();
        }

        private string GetFormattedValue(object value)
        {
            bool isNull = value == null;
            bool isString = value is string;
            bool isJson = isString && value.ToString().StartsWith("{") && value.ToString().EndsWith("}");
            bool isDate = value is DateTime;

            if (isNull) return null;

            if ((isString && !isJson) || isDate)
            {
                return string.Format(@"""{0}""", value);
            }

            return value.ToString();
        }
    }
}
