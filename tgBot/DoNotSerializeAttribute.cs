using System;

namespace tgBot
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class DoNotSerializeAttribute : Attribute
    {
        public DoNotSerializeAttribute() { }
    }
}
