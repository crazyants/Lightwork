using System;
using System.Collections.Generic;

namespace D3.Lightwork.Data
{
    [Serializable]
    public class WorkflowStoreState
    {
        public WorkflowStoreState()
        {
            Arguments = new Dictionary<string, object>();
        }

        public object State { get; set; }

        public IDictionary<string, object> Arguments { get; set; }

        public ICollection<object> Parameters { get; set; } 
    }
}
