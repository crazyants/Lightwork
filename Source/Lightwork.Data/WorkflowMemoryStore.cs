using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Lightwork.Core;

#pragma warning disable 1998

namespace Lightwork.Data
{
    public class WorkflowMemoryStore : WorkflowStore
    {
        private readonly ConcurrentBag<WorkflowSyncStateEntity> _syncStates =
            new ConcurrentBag<WorkflowSyncStateEntity>();

        private readonly object _syncLock = new object();

        public override async Task<bool> SyncState(
            string syncId,
            WorkflowInstance instance,
            bool createSync = true,
            bool setState = false)
        {
            lock (_syncLock)
            {
                var syncState = _syncStates.SingleOrDefault(s => s.SyncId == syncId && s.WorkflowId == instance.Id);
                if (syncState == null && createSync)
                {
                    _syncStates.Add(
                        new WorkflowSyncStateEntity
                        {
                            SyncId = syncId,
                            WorkflowId = instance.Id,
                            State = GetStoreState(instance)
                        });
                    return false;
                }

                return true;
            }
        }

        public override async Task InitializeContext(bool resetContext)
        {
        }

        public override async Task LoadFromStore()
        {
        }

        public override async Task StoreEvent(StoreEvents storeEvent, WorkflowInstance instance)
        {
        }
    }
}