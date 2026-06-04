using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;

namespace DAL.Data
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateAuditFields(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateAuditFields(DbContext? context)
        {
            if (context == null) return;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var now = DateTime.UtcNow;
                var entityType = entry.Entity.GetType();

                var updatedAtProp = entityType.GetProperty("UpdatedAt");
                if (updatedAtProp != null && updatedAtProp.CanWrite && entry.State == EntityState.Modified)
                {
                    updatedAtProp.SetValue(entry.Entity, now);
                }

                var createdAtProp = entityType.GetProperty("CreatedAt");
                if (createdAtProp != null && createdAtProp.CanWrite && entry.State == EntityState.Added)
                {
                    createdAtProp.SetValue(entry.Entity, now);
                }
            }
        }
    }
}
