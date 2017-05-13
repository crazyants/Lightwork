using System;
using System.Runtime.Serialization;

namespace D3.Lightwork.ServiceApi.Client
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
