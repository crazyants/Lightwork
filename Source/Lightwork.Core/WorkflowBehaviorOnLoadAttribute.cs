using System;

namespace D3.Lightwork.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WorkflowBehaviorOnLoadAttribute : Attribute
    {
        public bool BypassWorkflow { get; set; }
    }
}
