using System.Threading.Tasks;

namespace Lightwork.Core
{
    [WorkflowBehaviorOnLoad(BypassWorkflow = true)]
    public class EmptyWorkflow : Workflow
    {
#pragma warning disable 1998
        protected override async Task Execute(WorkflowInstance instance)
#pragma warning restore 1998
        {
        }
    }
}
