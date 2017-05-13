using System;
using System.Runtime.Serialization;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public class ActionWorkflowRequestContract : WorkflowRequestContract
    {
        public ActionWorkflowRequestContract()
        {
        }

        public ActionWorkflowRequestContract(Guid workflowId, string action)
            : base(workflowId)
        {
            Action = action;
        }

        public ActionWorkflowRequestContract(Guid workflowId, string action, string tag)
            : this(workflowId, action)
        {
            Tag = tag;
        }

        public ActionWorkflowRequestContract(Guid workflowId, string action, params ArgumentContract[] arguments)
            : this(workflowId, action)
        {
            Arguments = arguments;
        }

        public ActionWorkflowRequestContract(Guid workflowId, string action, string tag, params ArgumentContract[] arguments)
            : this(workflowId, action, tag)
        {
            Arguments = arguments;
        }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public string Tag { get; set; }

        [DataMember]
        public bool WaitOnAction { get; set; }
    }
}
