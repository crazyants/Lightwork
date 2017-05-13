using System.Data.Entity;

namespace D3.Lightwork.Data
{
    public class WorkflowDbContext : DbContext
    {
        public WorkflowDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public IDbSet<WorkflowSyncEventEntity> WorkflowSyncEvents { get; set; }

        public IDbSet<WorkflowSyncStateEntity> WorkflowSyncStates { get; set; }
    }
}
