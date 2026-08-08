using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SPIP.Application.Interfaces.Services;
using SPIP.Domain.Entities;
using System.Text.Json;

namespace SPIP.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var identityId = _currentUserService.UserId;
        int? domainUserId = null;

        if (identityId.HasValue)
        {
            // Try to find the domain user by IdentityId.
            // Be careful to use Local first to avoid querying if it's already tracked,
            // or we might just execute a simple synchronous query (not ideal in interceptor, but tolerable for this).
            var user = context.Set<User>().Local.FirstOrDefault(u => u.IdentityId == identityId.Value) 
                       ?? context.Set<User>().FirstOrDefault(u => u.IdentityId == identityId.Value);
            
            if (user != null)
            {
                domainUserId = user.Id;
            }
        }

        context.ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog &&
                        // Exclude AI chat message content — prompts/responses must not be persisted to audit logs
                        e.Entity is not Domain.Entities.AIChatMessage &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                UserId = domainUserId,
                Action = entry.State.ToString(),
                EntityName = entry.Metadata.Name,
                CreatedAt = DateTime.UtcNow
            };

            // Redact sensitive properties before serialisation (e.g. message Content on AIChatSession would be rare,
            // but we defensively exclude any property named "Content" or "Password" from audit details).
            var properties = entry.Properties
                .Where(p => !p.IsTemporary && p.Metadata.Name is not ("Content" or "PasswordHash" or "SecurityStamp"))
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

            // Try to extract an Id if it exists and is an integer
            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id" && p.CurrentValue is int);
            if (idProp != null)
            {
                auditLog.EntityId = (int?)idProp.CurrentValue;
            }

            auditLog.Details = JsonSerializer.Serialize(properties);

            auditEntries.Add(auditLog);
        }

        if (auditEntries.Any())
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }
}
