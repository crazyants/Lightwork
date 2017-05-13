using System;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Data
{
    public enum StoreEvents
    {
        NonEvent,
        CreateEvent,
        StartEvent,
        CompleteEvent,
        ErrorEvent
    }

    public interface IWorkflowStore : IDisposable
    {
        void SetWorkflowEngine(WorkflowEngine engine);

        Task<bool> SyncState(string syncId, WorkflowInstance instance, bool createSync = true, bool setState = false);
        
        Task InitializeContext(bool resetContext);

        Task LoadFromStore();

        Task StoreEvent(StoreEvents storeEvent, WorkflowInstance instance);
    }
}
