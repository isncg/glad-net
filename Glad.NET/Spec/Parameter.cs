using System.Collections.Generic;
using System.Xml;

namespace Glad.Spec
{
    public class Parameter : CommandItem
    {
        public string LengthParam { get; }

        public string Type { get; }
        public string Name { get; }

        public Parameter(XmlElement node) : base(node)
        {
            LengthParam = node.HasAttribute("len") ? node.GetAttribute("len") : null;
            Type = node["ptype"]?.InnerText;
            if (null != Type)
            {
                var sp = Type.Trim().Split(" ");
                if (null != sp && sp.Length>1)
                {
                    Type = null;
                }
            }
            Name = node["name"]?.InnerText;

        }
    }
}