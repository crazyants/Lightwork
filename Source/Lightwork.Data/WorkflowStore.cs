using System;
using System.Linq;
using System.Threading.Tasks;
using Lightwork.Core;
using Lightwork.Core.Utilities;

namespace Lightwork.Data
{
    public abstract class WorkflowStore : IWorkflowStore
    {
        protected WorkflowEngine WorkflowEngine { get; set; }

        public static IWorkflowStore TryGetStore(WorkflowInstance instance)
        {
            return instance.WorkflowEngine.GetProperty<IWorkflowStore>("Store");
        }

        public void SetWorkflowEngine(WorkflowEngine engine)
        {
            WorkflowEngine = engine;
        }

        public abstract Task<bool> SyncState(
            string syncId,
            WorkflowInstance instance,
            bool createSync = true,
            bool setState = false);

        public abstract Task InitializeContext(bool resetContext);

        public abstract Task LoadFromStore();

        public abstract Task StoreEvent(StoreEvents storeEvent, WorkflowInstance instance);
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual string GetStoreState(WorkflowInstance instance)
        {
            return JsonHelper.Serialize(
                new WorkflowStoreState
                {
                    State = instance.TryGetState(),
                    Arguments = instance.Arguments.ToDictionary(a => a.Name, a => a.Value)
                });
        }

        protected virtual void SetWorkflowState(WorkflowInstance instance, string storeState)
        {
            var state = JsonHelper.Deserialize<WorkflowStoreState>(storeState);

            if (state.State != null)
            {
                instance.TrySetState(state.State);
            }

            foreach (var arg in state.Arguments)
            {
                instance.Arguments.Single(a => a.Name == arg.Key).Value = arg.Value;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}