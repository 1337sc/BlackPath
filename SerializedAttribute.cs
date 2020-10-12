using System;
using System.IO;

namespace tgBot
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class SerializedAttribute : Attribute
    {
        public SerializedAttribute() { }
    }
}
