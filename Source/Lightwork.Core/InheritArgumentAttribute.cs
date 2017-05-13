using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D3.Lightwork.Core
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InheritArgumentAttribute : Attribute
    {
        public InheritArgumentAttribute()
        {
            CanInherit = true;
        }

        public InheritArgumentAttribute(bool canInherit)
        {
            CanInherit = canInherit;
        }

        public bool CanInherit { get; set; }
    }
}
