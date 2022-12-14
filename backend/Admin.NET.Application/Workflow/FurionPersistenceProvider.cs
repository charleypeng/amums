using Admin.NET.Core;
using Furion;
using Furion.DatabaseAccessor;
using Furion.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkflowCore.Models;

namespace Admin.NET.Application
{
    /// <summary>
    /// FurionPersistenceProvider
    /// </summary>
    [AllowAnonymous]
    public class FurionPersistenceProvider : IFurionPersistenceProvider, ISingleton
    {
        private readonly IRepository<PersistedEvent> _eventRepository = App.GetService<IRepository<PersistedEvent>>();
        private readonly IRepository<PersistedSubscription> _eventSubscriptionRepository = App.GetService<IRepository<PersistedSubscription>>();

        protected readonly IRepository<PersistedExecutionPointer> _executionPointerRepository = App.GetService<IRepository<PersistedExecutionPointer>>();
        protected readonly IRepository<PersistedWorkflow> _workflowRepository = App.GetService<IRepository<PersistedWorkflow>>();
        protected readonly IRepository<PersistedWorkflowDefinition> _workflowDefinitionRepository = App.GetService<IRepository<PersistedWorkflowDefinition>>();
        protected readonly IRepository<PersistedExecutionError> _executionErrorRepository = App.GetService<IRepository<PersistedExecutionError>>();

        protected readonly IRepository<PersistedScheduledCommand> _scheduledCommandRepository = App.GetService<IRepository<PersistedScheduledCommand>>();

        // protected readonly DbContext dbContext = Db.GetDbContext();

        public bool SupportsScheduledCommands => false;

        public async Task<string> CreateEventSubscription(EventSubscription subscription, CancellationToken cancellationToken = default)
        {
            //using (var db = Db.GetDbContext())
            //{
            //    subscription.Id = Guid.NewGuid().ToString();
            //    var persistable = subscription.ToPersistable();
            //    var result = db.Set<PersistedSubscription>().Add(persistable);
            //    await db.SaveChangesAsync(cancellationToken);
            //    return subscription.Id;
            //}

            subscription.Id = Guid.NewGuid().ToString();
            var persistable = subscription.ToPersistable();
            await _eventSubscriptionRepository.InsertNowAsync(persistable);
            return subscription.Id;
        }

        public async Task<string> CreateNewWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            //using (var db = Db.GetDbContext())
            //{
            //    workflow.Id = Guid.NewGuid().ToString();
            //    var persistable = workflow.ToPersistable();
            //    var result = db.Set<PersistedWorkflow>().Add(persistable);
            //    await db.SaveChangesAsync(cancellationToken);
            //    return workflow.Id;
            //}

            workflow.Id = Guid.NewGuid().ToString();
            var persistable = workflow.ToPersistable();
            await _workflowRepository.InsertNowAsync(persistable);
            return workflow.Id;
        }

        public async Task<IEnumerable<string>> GetRunnableInstances(DateTime asAt, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var now = asAt.Ticks;
                var raw = await db.Set<PersistedWorkflow>()
                    .Where(x => x.NextExecution.HasValue && (x.NextExecution <= now) && (x.Status == WorkflowStatus.Runnable))
                    .Select(x => x.InstanceId)
                    .ToListAsync(cancellationToken);

                return raw.Select(s => s.ToString()).ToList();
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(WorkflowStatus? status, string type, DateTime? createdFrom, DateTime? createdTo, int skip, int take)
        {
            using (var db = Db.GetDbContext())
            {
                IQueryable<PersistedWorkflow> query = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsQueryable();

                if (status.HasValue)
                    query = query.Where(x => x.Status == status.Value);

                if (!String.IsNullOrEmpty(type))
                    query = query.Where(x => x.WorkflowDefinitionId == type);

                if (createdFrom.HasValue)
                    query = query.Where(x => x.CreateTime >= createdFrom.Value);

                if (createdTo.HasValue)
                    query = query.Where(x => x.CreateTime <= createdTo.Value);

                var rawResult = await query.Skip(skip).Take(take).ToListAsync();
                List<WorkflowInstance> result = new List<WorkflowInstance>();

                foreach (var item in rawResult)
                    result.Add(item.ToWorkflowInstance());

                return result;
            }
        }

        public async Task<WorkflowInstance> GetWorkflowInstance(string Id, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(Id);
                var raw = await db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .FirstAsync(x => x.InstanceId == uid, cancellationToken);

                if (raw == null)
                    return null;

                return raw.ToWorkflowInstance();
            }
        }

        public async Task<IEnumerable<WorkflowInstance>> GetWorkflowInstances(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            if (ids == null)
            {
                return new List<WorkflowInstance>();
            }

            using (var db = Db.GetDbContext())
            {
                var uids = ids.Select(i => new Guid(i));
                var raw = db.Set<PersistedWorkflow>()
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .Where(x => uids.Contains(x.InstanceId));

                return (await raw.ToListAsync(cancellationToken)).Select(i => i.ToWorkflowInstance());
            }
        }

        public async Task PersistWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(workflow.Id);
                var existingEntity = await db.Set<PersistedWorkflow>()
                    .Where(x => x.InstanceId == uid)
                    .Include(wf => wf.ExecutionPointers)
                    .ThenInclude(ep => ep.ExtensionAttributes)
                    .Include(wf => wf.ExecutionPointers)
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                var persistable = workflow.ToPersistable(existingEntity);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task TerminateSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var existing = await db.Set<PersistedSubscription>().FirstAsync(x => x.SubscriptionId == uid, cancellationToken);
                db.Set<PersistedSubscription>().Remove(existing);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public virtual void EnsureStoreExists()
        {
        }

        public async Task<IEnumerable<EventSubscription>> GetSubscriptions(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var raw = await db.Set<PersistedSubscription>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf)
                    .ToListAsync(cancellationToken);

                return raw.Select(item => item.ToEventSubscription()).ToList();
            }
        }

        public async Task<string> CreateEvent(Event newEvent, CancellationToken cancellationToken = default)
        {
            //using (var db = Db.GetDbContext())
            //{
            //    newEvent.Id = Guid.NewGuid().ToString();
            //    var persistable = newEvent.ToPersistable();
            //    var result = db.Set<PersistedEvent>().Add(persistable);
            //    await db.SaveChangesAsync(cancellationToken);
            //    return newEvent.Id;
            //}

            newEvent.Id = Guid.NewGuid().ToString();
            var persistable = newEvent.ToPersistable();
            await _eventRepository.InsertNowAsync(persistable);
            return newEvent.Id;
        }

        public async Task<Event> GetEvent(string id, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                Guid uid = new Guid(id);
                var raw = await db.Set<PersistedEvent>()
                    .FirstAsync(x => x.EventId == uid, cancellationToken);

                if (raw == null)
                    return null;

                return raw.ToEvent();
            }
        }

        public async Task<IEnumerable<string>> GetRunnableEvents(DateTime asAt, CancellationToken cancellationToken = default)
        {
            var now = asAt;
            using (var db = Db.GetDbContext())
            {
                var raw = await db.Set<PersistedEvent>()
                    .Where(x => !x.IsProcessed)
                    .Where(x => x.EventTime <= now)
                    .Select(x => x.EventId)
                    .ToListAsync(cancellationToken);

                return raw.Select(s => s.ToString()).ToList();
            }
        }

        public async Task MarkEventProcessed(string id, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(id);
                var existingEntity = await db.Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                existingEntity.IsProcessed = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<IEnumerable<string>> GetEvents(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken)
        {
            using (var db = Db.GetDbContext())
            {
                var raw = await db.Set<PersistedEvent>()
                    .Where(x => x.EventName == eventName && x.EventKey == eventKey)
                    .Where(x => x.EventTime >= asOf)
                    .Select(x => x.EventId)
                    .ToListAsync(cancellationToken);

                var result = new List<string>();

                foreach (var s in raw)
                    result.Add(s.ToString());

                return result;
            }
        }

        public async Task MarkEventUnprocessed(string id, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(id);
                var existingEntity = await db.Set<PersistedEvent>()
                    .Where(x => x.EventId == uid)
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                existingEntity.IsProcessed = false;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task PersistErrors(IEnumerable<ExecutionError> errors, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var executionErrors = errors as ExecutionError[] ?? errors.ToArray();
                if (executionErrors.Any())
                {
                    foreach (var error in executionErrors)
                    {
                        db.Set<PersistedExecutionError>().Add(error.ToPersistable());
                    }
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }

        public async Task<EventSubscription> GetSubscription(string eventSubscriptionId, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var raw = await db.Set<PersistedSubscription>().FirstOrDefaultAsync(x => x.SubscriptionId == uid, cancellationToken);

                return raw?.ToEventSubscription();
            }
        }

        public async Task<EventSubscription> GetFirstOpenSubscription(string eventName, string eventKey, DateTime asOf, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var raw = await db.Set<PersistedSubscription>().FirstOrDefaultAsync(x => x.EventName == eventName && x.EventKey == eventKey && x.SubscribeAsOf <= asOf && x.ExternalToken == null, cancellationToken);

                return raw?.ToEventSubscription();
            }
        }

        public async Task<bool> SetSubscriptionToken(string eventSubscriptionId, string token, string workerId, DateTime expiry, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var existingEntity = await db.Set<PersistedSubscription>()
                    .Where(x => x.SubscriptionId == uid)
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                existingEntity.ExternalToken = token;
                existingEntity.ExternalWorkerId = workerId;
                existingEntity.ExternalTokenExpiry = expiry;
                await db.SaveChangesAsync(cancellationToken);

                return true;
            }
        }

        public async Task ClearSubscriptionToken(string eventSubscriptionId, string token, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var uid = new Guid(eventSubscriptionId);
                var existingEntity = await db.Set<PersistedSubscription>()
                    .Where(x => x.SubscriptionId == uid)
                    .AsTracking()
                    .FirstAsync(cancellationToken);

                if (existingEntity.ExternalToken != token)
                    throw new InvalidOperationException();

                existingEntity.ExternalToken = null;
                existingEntity.ExternalWorkerId = null;
                existingEntity.ExternalTokenExpiry = null;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task ScheduleCommand(ScheduledCommand command)
        {
            try
            {
                using (var db = Db.GetDbContext())
                {
                    var persistable = command.ToPersistable();
                    var result = db.Set<PersistedScheduledCommand>().Add(persistable);
                    await db.SaveChangesAsync();
                }
            }
            catch (DbUpdateException)
            {
                //log
            }
        }

        public async Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default)
        {
            using (var db = Db.GetDbContext())
            {
                var cursor = db.Set<PersistedScheduledCommand>()
                    .Where(x => x.ExecuteTime < asOf.UtcDateTime.Ticks)
                    .AsAsyncEnumerable();

                await foreach (var command in cursor)
                {
                    try
                    {
                        await action(command.ToScheduledCommand());
                        using (var db2 = Db.GetDbContext())
                        {
                            db2.Set<PersistedScheduledCommand>().Remove(command);
                            await db2.SaveChangesAsync();
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: add logger
                    }
                }
            }
        }
    }
}