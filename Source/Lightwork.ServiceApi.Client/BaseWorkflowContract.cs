using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Lightwork.Core;
using Lightwork.Core.Utilities;

namespace Lightwork.ServiceApi.Client
{
    [DataContract]
    public abstract class BaseWorkflowContract
    {
        protected BaseWorkflowContract()
        {
            Arguments = new List<ArgumentContract>();
        }

        protected BaseWorkflowContract(Guid workflowId)
            : this()
        {
            WorkflowId = workflowId;
        }

        [DataMember]
        public Guid WorkflowId { get; set; }

        [DataMember]
        public IList<ArgumentContract> Arguments { get; set; }

        public T GetArgument<T>(string name)
        {
            var argument = Arguments.SingleOrDefault(a => a.Name == name);
            return argument == null ? default(T) : TypeHelper.ChangeType<T>(argument.Value);
        }

        public Argument[] GetArguments()
        {
            return Arguments.Select(a => Argument.Create(a.Type, a.Name, a.Value)).ToArray();
        }
    }
}
