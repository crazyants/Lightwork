using System;
using System.Runtime.Serialization;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public abstract class WorkflowRequestContract : BaseWorkflowContract
    {
        protected WorkflowRequestContract()
        {
        }

        protected WorkflowRequestContract(Guid workflowId)
            : base(workflowId)
        {
        }
    }
}
