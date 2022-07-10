using System.Collections.Generic;
using System.Xml;

namespace Glad.Spec
{
    public class EnumMember : NamedEntry
    {
        public string Value { get; }

        public string Alias { get; }
        public string[] Group { get; }
        public string Type { get; }
        public Enumeration Parent { get; }
        public EnumMember(XmlElement node, Enumeration parent) : base(node)
        {
            Value = node.GetAttribute("value");
            Group = new List<string>(node.GetAttribute("group").Split(",")).FindAll(s=>!string.IsNullOrWhiteSpace(s)).ToArray();
            Type = node.GetAttribute("type");
            Parent = parent;
            if (string.IsNullOrWhiteSpace(Value))
                throw new XmlException("Value cannot be null/empty.");
            Alias = node.HasAttribute("alias") ? node.GetAttribute("alias") : null;
          
        }
    }
}