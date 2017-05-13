using System;
using System.Runtime.Serialization;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public class GetWorkflowRequestContract : WorkflowRequestContract
    {
        public GetWorkflowRequestContract()
        {
        }

        public GetWorkflowRequestContract(Guid workflowId)
            : base(workflowId)
        {
        }
    }
}
