using System;

namespace Lightwork.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WorkflowBehaviorOnLoadAttribute : Attribute
    {
        public bool BypassWorkflow { get; set; }
    }
}
