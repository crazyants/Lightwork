using System.Runtime.Serialization;
using Lightwork.Core;

namespace Lightwork.ServiceApi.Client
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
