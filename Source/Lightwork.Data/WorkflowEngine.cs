using System;
using System.Threading;
using System.Threading.Tasks;
using D3.Lightwork.Core;

namespace D3.Lightwork.Data
{
    public class WorkflowEngine<TStore> : WorkflowEngine where TStore : IWorkflowStore
    {
        public WorkflowEngine(TStore store)
            : this(store, false, false)
        {
        }

        public WorkflowEngine(TStore store, bool resetContext)
            : this(store, true, resetContext)
        {
        }

        public WorkflowEngine(TStore store, bool initializeContext, bool resetContext, bool disposeStore = true)
        {
            DisposeStore = disposeStore;
            Store = store;
            Store.SetWorkflowEngine(this);
            if (initializeContext)
            {
                Store.InitializeContext(resetContext);
            }
        }

        public TStore Store { get; set; }

        private bool DisposeStore { get; set; }

        public override WorkflowInstance CreateWorkflow(
            Workflow workflow,
            Guid id = new Guid(),
            Guid parentId = new Guid())
        {
            var instance = base.CreateWorkflow(workflow, id, parentId);
            Store.StoreEvent(StoreEvents.CreateEvent, instance);
            return instance;
        }

        public override WorkflowInstance CreateWorkflow<TWorkflow>(Guid id = new Guid(), Guid parentId = new Guid())
        {
            var instance = base.CreateWorkflow<TWorkflow>(id, parentId);
            Store.StoreEvent(StoreEvents.CreateEvent, instance);
            return instance;
        }

        public override WorkflowInstance<TState> CreateWorkflow<TWorkflow, TState>(
            Guid id = new Guid(),
            Guid parentId = new Guid())
        {
            var instance = base.CreateWorkflow<TWorkflow, TState>(id, parentId);
            Store.StoreEvent(StoreEvents.CreateEvent, instance);
            return instance;
        }

        protected override Task StartWorkflowTask(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            Store.StoreEvent(StoreEvents.StartEvent, instance);
            return base.StartWorkflowTask(instance, cancellationToken);
        }

        protected override void OnWorkflowComplete(object sender, WorkflowCompleteEventArgs e)
        {
            var instance = (WorkflowInstance)sender;
            Store.StoreEvent(StoreEvents.CompleteEvent, instance);
            base.OnWorkflowComplete(sender, e);
        }

        protected override void OnWorkflowException(object sender, WorkflowExceptionEventArgs e)
        {
            var instance = (WorkflowInstance)sender;
            Store.StoreEvent(StoreEvents.ErrorEvent, instance);
            base.OnWorkflowException(sender, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (DisposeStore)
                {
                    Store.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    public class DbStoreWorkflowEngine : WorkflowEngine<WorkflowDbStore>
    {
        public DbStoreWorkflowEngine()
            : base(new WorkflowDbStore())
        {
        }

        public DbStoreWorkflowEngine(bool resetContext)
            : base(new WorkflowDbStore(), resetContext)
        {
        }

        public DbStoreWorkflowEngine(bool initializeContext, bool resetContext)
            : base(new WorkflowDbStore(), initializeContext, resetContext)
        {
        }

        public DbStoreWorkflowEngine(string contextName)
            : base(new WorkflowDbStore(contextName), true)
        {
        }

        public DbStoreWorkflowEngine(string contextName, bool resetContext)
            : base(new WorkflowDbStore(contextName), resetContext)
        {
        }

        public DbStoreWorkflowEngine(string contextName, bool initializeContext, bool resetContext)
            : base(new WorkflowDbStore(contextName), initializeContext, resetContext)
        {
        }

        public DbStoreWorkflowEngine(WorkflowDbStore store)
            : base(store, false, false, false)
        {
        }

        public DbStoreWorkflowEngine(WorkflowDbStore store, bool resetContext)
            : base(store, true, resetContext, false)
        {
        }

        public DbStoreWorkflowEngine(WorkflowDbStore store, bool initializeContext, bool resetContext)
            : base(store, initializeContext, resetContext, false)
        {
        }
    }

    public class MemoryStoreWorkflowEngine : WorkflowEngine<WorkflowMemoryStore>
    {
        public MemoryStoreWorkflowEngine()
            : base(new WorkflowMemoryStore())
        {
        }

        public MemoryStoreWorkflowEngine(bool resetContext)
            : base(new WorkflowMemoryStore(), true, resetContext)
        {
        }
    }
}