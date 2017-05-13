using System;

namespace Lightwork.Core
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
