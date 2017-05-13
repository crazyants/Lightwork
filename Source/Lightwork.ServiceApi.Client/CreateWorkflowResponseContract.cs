using System.Runtime.Serialization;
using Lightwork.Core;

namespace Lightwork.ServiceApi.Client
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
