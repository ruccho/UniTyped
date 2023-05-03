

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

    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class UniTypedMaterialViewAttribute : System.Attribute
    {
        public string ShaderPath { get; }
        public UniTypedMaterialViewAttribute(string shaderPath)
        {
            this.ShaderPath = shaderPath;
        }
    }

    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class UniTypedAnimatorViewAttribute : System.Attribute
    {
        public string ControllerPath { get; }
        public UniTypedAnimatorViewAttribute(string controllerPath)
        {
            this.ControllerPath = controllerPath;
        }
    }
}
