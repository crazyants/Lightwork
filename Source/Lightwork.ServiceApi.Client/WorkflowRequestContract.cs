using System;
using System.Runtime.Serialization;

namespace Lightwork.ServiceApi.Client
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
