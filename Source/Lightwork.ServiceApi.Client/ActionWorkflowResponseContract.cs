using System.Runtime.Serialization;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class ActionWorkflowResponseContract : GetWorkflowResponseContract
    {
        public ActionWorkflowResponseContract()
        {
        }

        public ActionWorkflowResponseContract(WorkflowInstance instance, string action)
            : base(instance)
        {
            Action = action;
        }

        public ActionWorkflowResponseContract(WorkflowInstance instance, string action, string tag)
            : this(instance, action)
        {
            Tag = tag;
        }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public string Tag { get; set; }
    }
}
