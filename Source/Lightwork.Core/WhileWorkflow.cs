using System;
using System.Threading.Tasks;

namespace Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class WhileWorkflow : Workflow
    {
        public WhileWorkflow()
        {
        }

        public WhileWorkflow(Func<WorkflowInstance, Task> workflowActivity, Func<bool> condition)
        {
            WorkflowActivity = new Argument<Func<WorkflowInstance, Task>>("WorkflowActivity", workflowActivity);
            Condition = new Argument<Func<bool>>("Condition", condition);
        }

        [InheritArgument(false)]
        public Argument<Func<bool>> Condition { get; set; }

        [InheritArgument(false)]
        public Argument<Func<WorkflowInstance, Task>> WorkflowActivity { get; set; }
        
        protected override async Task Execute(WorkflowInstance instance)
        {
            while (!instance.IsCancelled && Condition.Value())
            {
                await instance.EnterWorkflow(Create(WorkflowActivity.Value));
            }
        }
    }

    public class WhileWorkflow<TWorkflow> : WhileWorkflow where TWorkflow : Workflow
    {
        public WhileWorkflow()
        {
        }

        public WhileWorkflow(Func<bool> condition)
            : base(null, condition)
        {
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            while (!instance.IsCancelled && Condition.Value())
            {
                await instance.EnterWorkflow<TWorkflow>();
            }
        }
    }
}
