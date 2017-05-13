using System;
using System.Threading.Tasks;
using D3.Lightwork.Core;

namespace D3.Lightwork.Data
{
    public class WorkflowStoreSync : IDisposable
    {
        private readonly IWorkflowStore _store;

        private readonly WorkflowInstance _instance;

        public WorkflowStoreSync(WorkflowInstance instance)
        {
            _store = WorkflowStore.TryGetStore(instance);
            _instance = instance;
        }

        public bool HasStore => _store != null;

        public async Task<bool> HasSync(string syncId)
        {
            return await _store.SyncState(syncId, _instance, false);
        }

        public async Task<bool> CreateSync(string syncId, bool setState = false)
        {
            return !await _store.SyncState(syncId, _instance, true, setState);
        }

        public async Task<bool> SyncState(string syncId)
        {
            return await _store.SyncState(syncId, _instance, true, true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}
