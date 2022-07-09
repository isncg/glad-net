using System.Collections.Generic;
using System.Xml;

namespace Glad.Spec
{
    public class Enumeration : EntryCollection<EnumMember>
    {
        public string Namespace { get; }
        public string Start { get; }
        public string End { get; }

        //public string Group { get; }

        public string Type { get; }

        public string Vendor { get; }
        //public string Comment { get;  }

        public Dictionary<string, Group> GroupDict { get; }

        public Enumeration(XmlElement node) : base(node)
        {
            Namespace = node.GetAttribute("namespace");
            //Group = node.HasAttribute("group") ? node.GetAttribute("group") : null;
            Type = node.HasAttribute("type") ? node.GetAttribute("type") : null;
            Vendor = node.HasAttribute("vendor") ? node.GetAttribute("vendor") : null;
            GroupDict = new Dictionary<string, Group>();
            foreach (XmlElement member in node.GetElementsByTagName("enum"))
            {
                var enumMember = new EnumMember(member);
                Add(enumMember);
                foreach(var groupName in enumMember.Group)
                {
                    Group group = null;
                    if (!GroupDict.TryGetValue(groupName, out group))
                    {
                        group = new Group();
                        GroupDict[enumMember.Name] = group;
                    }
                    group.Add(enumMember);
                    if (!string.IsNullOrWhiteSpace(enumMember.Type))
                        group.type.Add(enumMember.Type);
                }
            }
            if (!string.IsNullOrWhiteSpace(Type))
            {
                foreach (var kv in GroupDict)
                {
                    kv.Value.type.Add(Type);
                }
            }
        }
    }
}