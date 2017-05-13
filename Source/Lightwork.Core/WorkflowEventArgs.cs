using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lightwork.Core
{
    public class WorkflowCompleteEventArgs : EventArgs
    {
        public WorkflowCompleteEventArgs()
        {
            Arguments = new Collection<Argument>();
        }

        public ICollection<Argument> Arguments { get; internal set; }

        public Type WorkflowType { get; internal set; }

        public Guid WorkflowId { get; internal set; }

        public Guid ParentWorkflowId { get; set; }
    }

    public class WorkflowExceptionEventArgs : WorkflowCompleteEventArgs
    {
        public Exception Exception { get; set; }
    }
}