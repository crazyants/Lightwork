using System;
using System.Threading.Tasks;

namespace D3.Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class SequentialWorkflow : Workflow
    {
        public SequentialWorkflow()
        {
        }

        public SequentialWorkflow(Func<WorkflowInstance, Task> activity)
        {
            Activity = new Argument<Func<WorkflowInstance, Task>>("Activity", activity);
        }

        [InheritArgument(false)]
        public Argument<Func<WorkflowInstance, Task>> Activity { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            if (Activity.Value == null)
            {
                return;
            }

            await Activity.Value(instance);
        }
    }
}