using System.Threading.Tasks;

namespace Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class DelayWorkflow : Workflow
    {
        public DelayWorkflow()
        {
        }

        public DelayWorkflow(int timeoutDuration)
        {
            TimeoutDuration = new Argument<int>("TimeoutDuration", timeoutDuration);
        }
        
        public DelayWorkflow(Workflow workflow, int timeoutDuration)
            : this(timeoutDuration)
        {
            Workflow = new Argument<Workflow>("Workflow", workflow);
        }

        public Argument<int> TimeoutDuration { get; set; }

        [InheritArgument(false)]
        public Argument<Workflow> Workflow { get; set; }
        
        protected override async Task Execute(WorkflowInstance instance)
        {
            await WaitDelay();
            await EnterWorkflow();
        }

        protected async Task WaitDelay()
        {
            if (TimeoutDuration?.Value > 0)
            {
                await Task.Delay(TimeoutDuration.Value, WorkflowInstance.CancellationToken);
            }
        }

        protected virtual async Task EnterWorkflow()
        {
            if (Workflow.Value != null)
            {
                await WorkflowInstance.EnterWorkflow(Workflow.Value);
            }
        }
    }

    public class DelayWorkflow<TWorkflow> : DelayWorkflow where TWorkflow : Workflow
    {
        public DelayWorkflow()
        {
        }

        public DelayWorkflow(int timeoutDuration)
            : base(timeoutDuration)
        {
        }
        
        protected override async Task EnterWorkflow()
        {
            await WorkflowInstance.EnterWorkflow<TWorkflow>();
        }
    }
}
