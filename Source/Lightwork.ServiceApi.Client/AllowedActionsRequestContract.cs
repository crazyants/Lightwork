using System;
using System.Runtime.Serialization;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public class AllowedActionsRequestContract : GetWorkflowRequestContract
    {
        public AllowedActionsRequestContract()
        {
        }

        public AllowedActionsRequestContract(Guid workflowId)
            : base(workflowId)
        {
        }

        public AllowedActionsRequestContract(Guid workflowId, string tag)
            : base(workflowId)
        {
            Tag = tag;
        }

        [DataMember]
        public string Tag { get; set; }
    }
}
