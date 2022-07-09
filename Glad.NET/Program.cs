using System;
using System.Collections.Generic;
using System.Linq;
using Glad.Spec;

namespace Glad
{
    internal class Program
    {
        public const string GL_XML = @"https://raw.githubusercontent.com/KhronosGroup/OpenGL-Registry/master/xml/gl.xml";


        private static GlSpec spec;

        static void Main(string[] args)
        {


            spec = new GlSpec("gl.xml");
            spec.Parse();

            Generator.Generate(spec, Api.GL, Profile.Core, new Version(3, 3));

            Console.WriteLine("FINISHED");
            //Console.ReadLine();
        }

        private static IEnumerable<Api> ApiChoices()
        {
            var set = new HashSet<Api>();
            foreach (var feature in spec.Features)
                set.Add(feature.Api);
            set.Remove(Api.All);
            set.Remove(Api.Disabled);
            return set;
        }

        private static IEnumerable<Feature> FeatureChoices(Api api) => spec.Features.Where(f => f.Api.HasFlag(api));

        private static IEnumerable<Profile> GetProfiles(Api api, Version version)
        {
            var set = new HashSet<Profile>();
            foreach (var feature in spec.Features)
            {
                if (!feature.Api.HasFlag(api))
                    continue;
                if (feature.Version > version)
                    continue;

                foreach (var item in feature)
                {
                    set.Add(item.Profile);
                }
            }

            set.Remove(Profile.All);
            return set;
        }

    }
}
