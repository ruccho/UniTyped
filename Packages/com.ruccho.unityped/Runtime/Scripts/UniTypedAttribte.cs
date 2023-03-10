

using System;

namespace UniTyped
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class UniTypedAttribute : System.Attribute
    {
    }
    
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class UniTypedFieldAttribute : System.Attribute
    {
        public bool forceNested = false;
        public bool ignore = false;

        public UniTypedFieldAttribute()
        {
            
        }
    }
}
