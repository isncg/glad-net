using System.Xml;

namespace Glad.Spec
{
    public class EnumMember : NamedEntry
    {
        public string Value { get; }

        public string Alias { get; }
        public string[] Group { get; }
        public string Type { get; }

        public EnumMember(XmlElement node) : base(node)
        {
            Value = node.GetAttribute("value");
            Group = node.GetAttribute("group").Split(",");
            Type = node.GetAttribute("type");
            if (string.IsNullOrWhiteSpace(Value))
                throw new XmlException("Value cannot be null/empty.");
            Alias = node.HasAttribute("alias") ? node.GetAttribute("alias") : null;
          
        }
    }
}