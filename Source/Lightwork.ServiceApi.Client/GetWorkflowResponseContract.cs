using System.Runtime.Serialization;
using Lightwork.Core;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public class GetWorkflowResponseContract : WorkflowResponseContract
    {
        public GetWorkflowResponseContract()
        {
        }

        public GetWorkflowResponseContract(WorkflowInstance instance)
            : base(instance)
        {
            IsStarted = instance.IsStarted;
            IsRunning = instance.IsRunning;
            IsComplete = instance.IsComplete;
            IsCancelled = instance.IsCancelled;
            IsInAction = instance.IsInAction;
            IsInError = instance.IsInError;
        }

        [DataMember]
        public bool IsStarted { get; set; }

        [DataMember]
        public bool IsRunning { get; set; }

        [DataMember]
        public bool IsComplete { get; set; }

        [DataMember]
        public bool IsCancelled { get; set; }

        [DataMember]
        public bool IsInAction { get; set; }

        [DataMember]
        public bool IsInError { get; set; }
    }
}
