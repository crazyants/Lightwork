using System;

namespace D3.Lightwork.Core
{
    public class DelayUntilWorkflow : DelayWorkflow
    {
        public DelayUntilWorkflow()
        {
        }

        public DelayUntilWorkflow(Workflow workflow, DateTime escalationDateTime)
            : base(workflow, (int)escalationDateTime.Subtract(DateTime.Now).TotalMilliseconds)
        {
        }
    }

    public class DelayUntilWorkflow<TWorkflow> : DelayWorkflow<TWorkflow> where TWorkflow : Workflow
    {
        public DelayUntilWorkflow()
        {
        }

        public DelayUntilWorkflow(DateTime escalationDateTime)
            : base((int)escalationDateTime.Subtract(DateTime.Now).TotalMilliseconds)
        {
        }
    }
}