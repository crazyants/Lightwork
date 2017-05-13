using System;
using System.Runtime.Serialization;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class StartWorkflowRequestContract : WorkflowRequestContract
    {
        public StartWorkflowRequestContract()
        {
        }

        public StartWorkflowRequestContract(Guid workflowId)
            : base(workflowId)
        {
        }

        public StartWorkflowRequestContract(Guid workflowId, params ArgumentContract[] arguments)
            : this(workflowId)
        {
            Arguments = arguments;
        }

        [DataMember]
        public bool WaitOnComplete { get; set; }
    }
}
