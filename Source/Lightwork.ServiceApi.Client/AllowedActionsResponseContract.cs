using System.Collections.Generic;
using System.Runtime.Serialization;
using Lightwork.Core;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public class AllowedActionsResponseContract : GetWorkflowResponseContract
    {
        public AllowedActionsResponseContract()
        {
        }

        public AllowedActionsResponseContract(WorkflowInstance instance, ICollection<string> actions)
            : base(instance)
        {
            Actions = actions;
        }

        public AllowedActionsResponseContract(WorkflowInstance instance, string tag, ICollection<string> actions)
            : base(instance)
        {
            Actions = actions;
            Tag = tag;
        }

        [DataMember]
        public ICollection<string> Actions { get; set; }

        [DataMember]
        public string Tag { get; set; }
    }
}