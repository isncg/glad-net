using System.Collections.Generic;
using System.Xml;

namespace Glad.Spec
{
    public class Group : List<EnumMember>
    {
        public string Name;
        public HashSet<string> type;

        public Group()
        {
            type = new HashSet<string>();
        }
        public Group(Group other)
        {
            this.Name = new string(other.Name);
            this.type = new HashSet<string>(other.type);
            this.AddRange(other);
        }
    }
}