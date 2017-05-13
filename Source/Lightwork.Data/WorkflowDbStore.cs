using System;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using D3.Lightwork.Core;
using D3.Lightwork.Core.Utilities;

namespace D3.Lightwork.Data
{
    public class WorkflowDbStore : WorkflowStore
    {
        private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public WorkflowDbStore() : this("LightworkContext")
        {
        }

        public WorkflowDbStore(string contextName)
            : this(new WorkflowDbContext(contextName))
        {
        }

        public WorkflowDbStore(WorkflowDbContext context)
        {
            Context = context;
        }

        public WorkflowDbContext Context { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if instance was updated from sync state, false otherwise (stored state)</returns>
        public override async Task<bool> SyncState(
            string syncId,
            WorkflowInstance instance,
            bool createSync = true,
            bool setState = false)
        {
            await _syncLock.WaitAsync();
            var syncState =
                await
                    Context.WorkflowSyncStates.SingleOrDefaultAsync(
                        s => s.SyncId == syncId && s.WorkflowId == instance.Id);

            if (syncState != null)
            {
                if (setState)
                {
                    SetWorkflowState(instance, syncState.State);
                }

                _syncLock.Release();
                return true;
            }

            if (!createSync)
            {
                _syncLock.Release();
                return false;
            }

            Context.WorkflowSyncStates.Add(
                new WorkflowSyncStateEntity
                {
                    SyncId = syncId,
                    WorkflowId = instance.Id,
                    State = GetStoreState(instance),
                    DateCreated = DateTime.UtcNow
                });

            await Context.SaveChangesAsync();
            _syncLock.Release();

            return false;
        }

        public override async Task InitializeContext(bool resetContext)
        {
            await _syncLock.WaitAsync();
            IDatabaseInitializer<WorkflowDbContext> initializer = resetContext
                ? new DropCreateDatabaseAlways<WorkflowDbContext>()
                : null;
            Database.SetInitializer(initializer);
            Context.Database.Initialize(resetContext);
            _syncLock.Release();
        }

        public override async Task LoadFromStore()
        {
            await _syncLock.WaitAsync();
            var workflows = await Context.WorkflowSyncEvents.GroupBy(
                e => new { e.WorkflowId, e.WorkflowParentId, e.WorkflowType },
                e => new { e.StoreEvent, e.State },
                (workflowInfo, events) => new
                {
                    WorkflowInfo = workflowInfo,
                    Events = events
                }).Where(we => we.Events.All(e => e.StoreEvent != StoreEvents.CompleteEvent)).ToListAsync();

            foreach (var workflow in workflows)
            {
                var createEvent = workflow.Events.SingleOrDefault(e => e.StoreEvent == StoreEvents.CreateEvent);
                if (createEvent == null)
                {
                    continue;
                }

                var createState = JsonHelper.Deserialize<WorkflowStoreState>(createEvent.State);
                var wf = WorkflowEngine.CreateWorkflowType(workflow.WorkflowInfo.WorkflowType, createState.Parameters);

                var workflowBehavior =
                    wf.GetType()
                        .GetCustomAttributes(typeof(WorkflowBehaviorOnLoadAttribute), true)
                        .Cast<WorkflowBehaviorOnLoadAttribute>()
                        .SingleOrDefault();
                if (workflowBehavior != null && workflowBehavior.BypassWorkflow)
                {
                    continue;
                }

                var instance = WorkflowEngine.CreateWorkflow(
                    wf,
                    workflow.WorkflowInfo.WorkflowId,
                    workflow.WorkflowInfo.WorkflowParentId);

                var startEvent = workflow.Events.SingleOrDefault(e => e.StoreEvent == StoreEvents.StartEvent);
                if (startEvent == null)
                {
                    continue;
                }

                var startSyncState = JsonHelper.Deserialize<WorkflowStoreState>(startEvent.State);

                var startArgs =
                    startSyncState.Arguments.Where(arg => arg.Value != null)
                        .Select(arg => Argument.Create(arg.Value.GetType(), arg.Key, arg.Value))
                        .ToArray();

                var startState = startSyncState.State;
                await instance.Start(startState, startArgs);
            }

            _syncLock.Release();
        }

        public override async Task StoreEvent(StoreEvents storeEvent, WorkflowInstance instance)
        {
            await _syncLock.WaitAsync();
            var eventExists =
                await
                    Context.WorkflowSyncEvents.AnyAsync(e => e.WorkflowId == instance.Id && e.StoreEvent == storeEvent);

            if (eventExists)
            {
                _syncLock.Release();
                return;
            }

            var entity = new WorkflowSyncEventEntity
            {
                WorkflowId = instance.Id,
                StoreEvent = storeEvent,
                WorkflowType = instance.WorkflowType.AssemblyQualifiedName,
                WorkflowParentId = instance.ParentId,
                State = GetStoreState(instance),
                DateCreated = DateTime.UtcNow
            };
            Context.WorkflowSyncEvents.Add(entity);

            await Context.SaveChangesAsync();
            _syncLock.Release();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Context.Dispose();
                _syncLock.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}