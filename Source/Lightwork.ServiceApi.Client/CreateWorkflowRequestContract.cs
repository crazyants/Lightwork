using System.Collections.Generic;
using System.Runtime.Serialization;
using D3.Lightwork.Core;

namespace D3.Lightwork.ServiceApi.Client
{
    [DataContract]
    public class CreateWorkflowRequestContract : WorkflowRequestContract
    {
        public CreateWorkflowRequestContract()
        {
            StartImmediately = true;
        }

        public CreateWorkflowRequestContract(string workflowType)
            : this()
        {
            WorkflowType = workflowType;
        }

        public CreateWorkflowRequestContract(string workflowType, params ArgumentContract[] arguments)
            : this(workflowType)
        {
            Arguments = arguments;
        }

        public CreateWorkflowRequestContract(
            string workflowType,
            ICollection<object> parameters,
            params ArgumentContract[] arguments)
            : this(workflowType, arguments)
        {
            Parameters = parameters;
        }

        [DataMember]
        public string WorkflowType { get; set; }

        [DataMember]
        public bool StartImmediately { get; set; }

        [DataMember]
        public bool WaitOnComplete { get; set; }

        [DataMember]
        public ICollection<object> Parameters { get; set; }
    }

    [DataContract]
    public class CreateWorkflowRequestContract<TWorkflow> : CreateWorkflowRequestContract where TWorkflow : Workflow
    {
        public CreateWorkflowRequestContract()
            : base(typeof(TWorkflow).AssemblyQualifiedName)
        {
        }

        public CreateWorkflowRequestContract(params ArgumentContract[] arguments)
            : base(typeof(TWorkflow).AssemblyQualifiedName, arguments)
        {
        }

        public CreateWorkflowRequestContract(ICollection<object> parameters, params ArgumentContract[] arguments)
            : base(typeof(TWorkflow).AssemblyQualifiedName, parameters, arguments)
        {
        }
    }
}