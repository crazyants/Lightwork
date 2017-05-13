using System;
using System.Threading.Tasks;
using D3.Lightwork.Core;

namespace D3.Lightwork.Data
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
