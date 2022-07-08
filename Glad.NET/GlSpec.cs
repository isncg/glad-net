using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Glad.Spec;

namespace Glad
{
    public sealed class GlSpec
    {
        private readonly XmlElement root;

        public List<Group> Groups { get; }

        public List<Enumeration> Enums { get; }

        public List<Command> Commands { get; }
        
        public List<Feature> Features { get; }
        
        public List<Extension> Extensions { get; }

        public GlSpec(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            root = doc.DocumentElement;

            Groups = new List<Group>();
            Enums = new List<Enumeration>();
            Commands = new List<Command>();
            Features = new List<Feature>();
            Extensions = new List<Extension>();
        }

        public void Parse()
        {
            ParseGroups();
            ParseEnums();
            ParseCommands();
            ParseFeatures();
            ParseExtensions();
        }

        private void ParseExtensions()
        {
            Extensions.Clear();
            foreach (XmlElement ext in root["extensions"].GetElementsByTagName("extension"))
            {
                Extensions.Add(new Extension(ext));
            }
        }

        private void ParseFeatures()
        {
            Features.Clear();
            foreach (var feature in root.GetElementsByTagName("feature"))
            {
                if (feature is XmlElement entry)
                    Features.Add(new Feature(entry));
            }
        }

        public async Task ParseAsync() => await new Task(Parse);

        private void ParseGroups()
        {
            Groups.Clear();
            foreach (XmlElement group in root["groups"].GetElementsByTagName("group"))
                Groups.Add(new Group(group));
        }

        private void ParseEnums()
        {
            Enums.Clear();
            foreach (XmlElement node in root.GetElementsByTagName("enums"))
                Enums.Add(new Enumeration(node));
        }

        private void ParseCommands()
        {
            Commands.Clear();
            foreach (XmlElement node in root["commands"].GetElementsByTagName("command"))
                Commands.Add(new Command(node));
        }

        public IEnumerable<Command> GetCommands(Api api, Version version, Profile profile)
        {
            foreach (var name in Fetch(api, version, profile, FeatureType.Command))
                yield return Commands.Find(cmd => cmd.Name.Equals(name, StringComparison.Ordinal));
        }

        public IEnumerable<Command> GetExtensionCommands(Api api)
        {
            foreach (var ext in Extensions)
            {
                if ((ext.Supported & api) > 0)
                {
                    var i = ext.GetEnumerator();
                    while (i.MoveNext())
                    {
                        var extName = i.Current.Name;
                        var command = Commands.Find(cmd => cmd.Name.Equals(extName, StringComparison.Ordinal));
                        if (null != command)
                        {                         
                            yield return command;
                        }
                    }
                }
            }
        }

        public IEnumerable<EnumMember> GetEnums(Api api, Version version, Profile profile)
        {
            var allEnums = Enums.SelectMany(e => e).ToList();
            foreach (var name in Fetch(api, version, profile, FeatureType.Enum))
            {
                foreach (var member in allEnums)
                {
                    if (!member.Name.Equals(name, StringComparison.Ordinal)) continue;
                    yield return member;
                    break;
                }
            }
                
        }

        private IEnumerable<string> Fetch(Api api, Version version, Profile profile, FeatureType type) 
        {
            var set = new HashSet<string>();
            foreach (var feature in Features)
            {
                if (feature.Version > version)
                    continue;
                if (!feature.Api.HasFlag(api))
                    continue;

                foreach (var item in feature)
                {
                    if (!item.Type.HasFlag(type))
                        continue;
                   
                    if (!item.Profile.HasFlag(profile))
                        continue;

                    if (item.Action == FeatureAction.Require)
                        set.Add(item.Name);
                    if (item.Action == FeatureAction.Remove)
                        set.Remove(item.Name);
                }
            }

            return set;
        }
    }
}