using System;
using System.Runtime.Serialization;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public abstract class WorkflowResponseContract : BaseWorkflowContract
    {
        protected WorkflowResponseContract()
        {
        }

        protected WorkflowResponseContract(Guid workflowId, Guid parentWorkflowId, string workflowType)
            : base(workflowId)
        {
            ParentWorkflowId = parentWorkflowId;
            WorkflowType = workflowType;
        }

        protected WorkflowResponseContract(WorkflowInstance instance)
            : this(instance.Id, instance.ParentId, instance.WorkflowType.Name)
        {
            WorkflowState = instance.TryGetState();

            foreach (var arg in instance.Arguments)
            {
                Arguments.Add(ArgumentContract.Create(arg.Name, arg.Value));
            }
        }

        [DataMember]
        public Guid ParentWorkflowId { get; set; }

        [DataMember]
        public string WorkflowType { get; set; }

        [DataMember]
        public object WorkflowState { get; set; }
    }
}