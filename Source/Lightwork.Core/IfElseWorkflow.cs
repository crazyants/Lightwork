using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace D3.Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class IfElseWorkflow : Workflow
    {
        public IfElseWorkflow(bool breakAfterFirstTrue = true)
        {
            Workflows = new Argument<IList<Tuple<Workflow, Func<bool>>>>(
                "Workflows",
                new List<Tuple<Workflow, Func<bool>>>());

            BreakAfterFirstTrue = new Argument<bool>("BreakAfterFirstTrue", breakAfterFirstTrue);
        }

        [InheritArgument(false)]
        public Argument<IList<Tuple<Workflow, Func<bool>>>> Workflows { get; set; }

        public Argument<bool> BreakAfterFirstTrue { get; set; }

        public void AddBranch(Workflow workflow, Func<bool> condition = null, int branchIndex = -1)
        {
            var branch = Tuple.Create(workflow, condition);
            if (branchIndex > -1)
            {
                Workflows.Value.Insert(branchIndex, branch);
            }
            else
            {
                Workflows.Value.Add(branch);
            }
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            var ifHit = false;
            foreach (var branch in Workflows.Value.Where(x => x.Item2 != null).Where(branch => branch.Item2()))
            {
                ifHit = true;
                await instance.EnterWorkflow(branch.Item1);
                if (BreakAfterFirstTrue.Value)
                {
                    break;
                }
            }

            if (ifHit)
            {
                return;
            }

            foreach (var branch in Workflows.Value.Where(x => x.Item2 == null))
            {
                await instance.EnterWorkflow(branch.Item1);
            }
        }
    }
}