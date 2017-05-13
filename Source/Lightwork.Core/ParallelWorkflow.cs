using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class ParallelWorkflow : Workflow
    {
        private readonly ICollection<Tuple<Workflow, Argument[]>> _workflows =
            new Collection<Tuple<Workflow, Argument[]>>();

        public void Add(Workflow workflow, params Argument[] arguments)
        {
            AddWorkflow(workflow, arguments);
        }

        public void Add<TWorkflow>(params Argument[] arguments) where TWorkflow : Workflow
        {
            AddWorkflow(WorkflowEngine.CreateWorkflowType<TWorkflow>(), arguments);
        }

        protected override async Task Execute(WorkflowInstance instance)
        {
            var instances = new List<WorkflowInstance>();
            foreach (var workflowWithArgs in _workflows)
            {
                instances.Add(await instance.EnterWorkflow(workflowWithArgs.Item1, false, workflowWithArgs.Item2));
            }

            await Task.WhenAll(instances.Select(i => i.Wait()));
        }

        private void AddWorkflow(Workflow workflow, params Argument[] arguments)
        {
            _workflows.Add(new Tuple<Workflow, Argument[]>(workflow, arguments));
        }
    }
}