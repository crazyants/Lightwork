using System;
using System.Threading;
using System.Threading.Tasks;
using D3.Lightwork.Core;

namespace D3.Lightwork.Tests.Workflows
{
    public class PerformanceTestWorkflows
    {
    }

    public class IncrementWorkflow : Workflow
    {
        public Argument<int> Number { get; set; }

        public Argument<int> Delay { get; set; }

        public Argument<bool> UseTaskDelay { get; set; }

        public Argument<bool> UseWorkflowDelay { get; set; }

        protected override async Task Execute(WorkflowInstance instance)
        {
            if (Delay.Value > 0)
            {
                Func<Task> delayFunc;
                if (UseTaskDelay.Value)
                {
                    delayFunc = async () => await Task.Delay(Delay.Value);
                }
                else
                {
                    delayFunc = async () => await Task.Run(() => Thread.Sleep(Delay.Value)); 
                }

                if (UseWorkflowDelay.Value)
                {
                    await instance.EnterWorkflow(Create(delayFunc));
                }
                else
                {
                    await delayFunc();
                }
            }

            Number.Lock(() => { Number.Value += 1; });
        }
    }
}
