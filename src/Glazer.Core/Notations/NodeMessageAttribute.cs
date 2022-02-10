using Glazer.Core.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glazer.Core.Notations
{
    /// <summary>
    /// Marks the class type as a message.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeMessageAttribute : Attribute
    {
        /// <summary>
        /// Initialize a new <see cref="NodeMessageAttribute"/>.
        /// </summary>
        /// <param name="Name"></param>
        public NodeMessageAttribute(string Name = null) => this.Name = Name;

        /// <summary>
        /// Name overrides.
        /// </summary>
        public string Name { get; }
    }
}
