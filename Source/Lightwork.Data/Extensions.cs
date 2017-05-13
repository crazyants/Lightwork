using System;
using System.Threading.Tasks;
using Lightwork.Core;

namespace Lightwork.Data
{
    public static class Extensions
    {
        public static async Task WithStoreSync(this WorkflowInstance instance, Func<WorkflowStoreSync, Task> syncAction)
        {
            using (var storeSync = new WorkflowStoreSync(instance))
            {
                if (storeSync.HasStore)
                {
                    await syncAction(storeSync);
                }
            }
        }
    }
}
