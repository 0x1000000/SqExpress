using System;

namespace SqExpress
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SqModelAttribute : Attribute
    {
        public SqModelAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public string? PropertyName { get; set;  }

        public Type? CastType { get; set;  }
    }
}