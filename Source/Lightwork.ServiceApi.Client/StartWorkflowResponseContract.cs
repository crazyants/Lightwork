using System.Runtime.Serialization;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class StartWorkflowResponseContract : GetWorkflowResponseContract
    {
        public StartWorkflowResponseContract()
        {
        }

        public StartWorkflowResponseContract(WorkflowInstance instance)
            : base(instance)
        {
        }
    }
}
