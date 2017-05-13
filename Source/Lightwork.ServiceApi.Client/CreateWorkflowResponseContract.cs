using System.Runtime.Serialization;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class CreateWorkflowResponseContract : GetWorkflowResponseContract
    {
        public CreateWorkflowResponseContract()
        {
        }

        public CreateWorkflowResponseContract(WorkflowInstance instance)
            : base(instance)
        {
        }
    }
}
