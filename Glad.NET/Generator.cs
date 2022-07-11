using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Glad.Spec;

namespace Glad
{
    public static class Generator
    {
        private static readonly Dictionary<string, string> WORD_REPLACE;
        private static readonly Dictionary<string, string> TYPE_REPLACE;
        private static readonly Dictionary<string, string> NAME_REPLACE;
        private static readonly Dictionary<string, string> ENUMTYPE_REPLACE;

        static Generator()
        {
            WORD_REPLACE = new Dictionary<string, string>()
            {
                { "nv", "NV" },
                { "arb", "ARB" },
                { "amd", "AMD" },
                { "ati", "ATI" },
                { "ext", "EXT" },
                { "qcom", "QCOM" },
                { "pgi", "PGI" },
                { "intel", "INTEL" },
                { "apple", "APPLE" },
                { "mesa", "MESA" },
                { "1d", "1D"  },
                { "2d", "2D" },
                { "3d", "3D" },
                { "3dfx", "3DFX" },
                { "sgix", "SGIX" },
                { "oes", "OES" },
                { "2x", "TwoX" },
                { "4x", "FourX" },
                { "8x", "EightX" }
            };
            TYPE_REPLACE = new Dictionary<string, string>
            {
                { "GLboolean", "bool" },
                { "GLvoid", "void" },
                { "GLbyte", "byte" },
                { "GLchar", "byte" },
                { "GLcharARB", "byte" },
                { "GLubyte", "byte" },
                { "GLshort", "short" },
                { "GLushort", "ushort" },
                { "GLhalf", "ushort" },
                { "GLhalfARB", "ushort" },
                { "GLhalfNV", "ushort" },
                { "GLhandleARB", "ushort" },
                { "GLint", "int" },
                { "GLclampx", "int" },
                { "GLsizei", "int" },
                { "GLfixed", "int" },
                { "GLuint", "uint" },
                { "GLfloat", "float" },
                { "GLclampf", "float" },
                { "GLdouble", "double" },
                { "GLclampd", "double" },
                { "GLeglClientBufferEXT", "IntPtr" },
                { "GLeglImageOES", "IntPtr" },
                { "GLintptr", "IntPtr" },
                { "GLintptrARB", "IntPtr" },
                { "GLvdpauSurfaceNV", "IntPtr" },
                { "GLsizeiptr", "IntPtr" },
                { "GLsizeiptrARB", "IntPtr" },
                { "GLsync", "IntPtr" },
                { "GLint64", "long" },
                { "GLint64EXT", "long" },
                { "GLuint64", "ulong" },
                { "GLuint64EXT", "ulong" },
                { "GLDEBUGPROC", "DebugProc" },
                { "GLDEBUGPROCARB", "DebugProc" },
                { "GLDEBUGPROCKHR", "DebugProc" },
                { "GLDEBUGPROCAMD", "DebugProcAMD" },
                { "GLVULKANPROCNV", "VulkanDebugProcNV" },
            };
            NAME_REPLACE = new Dictionary<string, string>
            {
                { "in", "input" },
                { "out", "output" },
                { "params", "parameters" },
                { "string", "str" },
                { "ref", "reference" },
                { "object", "obj" },
                { "event", "evnt" }
            };
            ENUMTYPE_REPLACE = new Dictionary<string, string>
            {
                { "bitmask", "uint"},
                { "u", "uint"},
                { "ull", "ulong"}
            };


        }


        public static void Generate(GlSpec spec, Api api, Profile profile, Version version)
        {
            foreach (var e in spec.Enums)
            {
                var vendor = e.Vendor?.ToLowerInvariant();
                if (vendor is null || WORD_REPLACE.ContainsKey(vendor))
                    continue;
                WORD_REPLACE[vendor] = vendor.ToUpperInvariant();
            }

            using (var sw = new StreamWriter("OpenGL.cs"))
            {
                using (var writer = new IndentedTextWriter(sw))
                {
                    writer.WriteLine("using System;");
                    writer.WriteLine("using System.Runtime.InteropServices;");
                    writer.WriteLine("using System.Security;");
                    writer.WriteLine("using System.Diagnostics.CodeAnalysis;");
                    writer.WriteLine();
                    writer.WriteLine("namespace OpenGL");
                    writer.WriteLine("{");
                    writer.Indent++;
                    writer.WriteLine("public delegate IntPtr GetProcAddressHandler(string funcName);");
                    writer.WriteLine("public delegate void DebugProc(int source, int type, uint id, int severity, IntPtr length, byte[] message, IntPtr userParam);");
                    writer.WriteLine("public delegate void DebugProcAMD(uint id, int category, int severity, IntPtr length, byte[] message, IntPtr userParam);");
                    writer.WriteLine("public delegate void VulkanDebugProcNV();");
                    writer.WriteLine();

                    //var groups = GenerateEnums(spec, api, profile, version, writer);
                    GenerateEnums(spec, api, profile, version, writer);

                    //GenerateGroups(spec, groups, writer);

                    writer.WriteLine();

                    writer.WriteLine("[SuppressUnmanagedCodeSecurity]");
                    writer.WriteLine("[SuppressMessage(\"ReSharper\", \"StringLiteralTypo\")]");
                    writer.WriteLine("[SuppressMessage(\"ReSharper\", \"IdentifierTypo\")]");
                    writer.WriteLine("[SuppressMessage(\"ReSharper\", \"InconsistentNaming\")]");



                    writer.WriteLine("public static unsafe partial class Gl");
                    writer.WriteLine("{");
                    GenerateRawEnums(spec, writer);
                    writer.Indent++;

                    writer.WriteLine("public static T GetDelegateForFunctionPointer<T>(GetProcAddressHandler loader, string name)");
                    writer.WriteLine("{");
                    writer.WriteLine("    var addr = loader.Invoke(name);");
                    writer.WriteLine("    if (addr == IntPtr.Zero)");
                    writer.WriteLine("        return default(T);");
                    writer.WriteLine("    return Marshal.GetDelegateForFunctionPointer<T>(addr);");
                    writer.WriteLine("}");
                   
                    var imports = GenerateCommands(spec, api, profile, version, writer);

                    writer.WriteLine("public static void Initialize(GetProcAddressHandler loader)");
                    writer.WriteLine("{");
                    writer.Indent++;
                    foreach (var import in imports)
                        writer.WriteLine(import);

                    writer.Indent--;
                    writer.WriteLine("}");

                    writer.Indent--;
                    writer.WriteLine("}");

                    writer.Indent--;
                    writer.WriteLine("}");
                }
            }
        }

        public static void GenerateRawEnums(GlSpec spec, IndentedTextWriter writer)
        {
            Dictionary<string, EnumMember> nameDict = new Dictionary<string, EnumMember>();
            foreach (var enumeration in spec.Enums)
            {
                //foreach (var kv in enumeration.GroupDict)
                //{
                    foreach(var member in enumeration)
                    {
                        if (nameDict.ContainsKey(member.Name))
                            continue;
                        nameDict[member.Name] = member;
                    }                  
                //}
            }
            //writer.WriteLine($"public static class GlEnum");
            //writer.WriteLine("{");
            //writer.Indent++;
            foreach (var kv in nameDict)
            {
                EnumMember member = kv.Value;
                string memberType = member.Type;
                if (string.IsNullOrEmpty(memberType))
                    memberType = member.Parent.Type;
                string type = "int";
                if (!string.IsNullOrEmpty(memberType))
                    type = ENUMTYPE_REPLACE[memberType];
                else if (member.Value.StartsWith("0x") && member.Value.Length == 10 && member.Value[2] >= '8')
                    type = "uint";
                writer.WriteLine($"public static {type} {member.Name} = {member.Value};");
            }
            //writer.Indent--;
            //writer.WriteLine("}");
        }


        public static IEnumerable<Spec.Group> GenerateEnums(GlSpec spec, Api api, Profile profile, Version version, IndentedTextWriter writer)
        {
            //var groups = new List<Spec.Group>(spec.Groups);
            Dictionary<string, Spec.Group> groupDict = new Dictionary<string, Spec.Group>();
            foreach (var enumeration in spec.Enums)
            {
                foreach(var kv in enumeration.GroupDict)
                {
                    if(string.IsNullOrWhiteSpace(kv.Key))
                    {
                        Console.WriteLine("EMPTY KEY " + kv.Value.Name);
                        continue;
                    }
                    if(groupDict.TryGetValue(kv.Key, out var glist))
                    {
                        glist.AddRange(kv.Value);
                    }
                    else
                    {
                        groupDict[kv.Key] = new Spec.Group(kv.Value);
                    }
                }
            }

            foreach (var kv in groupDict)
            {
                string enumType = null;
                List<string> typeNames = new List<string>(kv.Value.type);
                if (typeNames.Count > 0)
                {
                    if (typeNames.Count > 1)
                    {
                        Console.WriteLine("ENUMTYPE: " + string.Join(",", typeNames) + " of " + kv.Key);
                        enumType = "error";
                    }
                    enumType = ENUMTYPE_REPLACE[typeNames[0]];
                }

                bool isSpecialNumbers = kv.Key.Equals("SpecialNumbers", StringComparison.Ordinal);
                if (isSpecialNumbers)
                {
                    writer.WriteLine($"public static class {kv.Key}");
                }
                else
                {
                    if (null == enumType)
                        writer.WriteLine($"public enum {kv.Key}");
                    else
                        writer.WriteLine($"public enum {kv.Key} : {enumType}");
                }
                writer.WriteLine("{");
                writer.Indent++;

                var count = 0;
                Spec.Group members = new Spec.Group(kv.Value);
                members.Sort((a, b) => a.Value.CompareTo(b.Value));
                HashSet<string> nameSet = new HashSet<string>();


                foreach (var member in members)
                {
                    count++;
                    var name = EnumMemberName(member.Name);
                    if (nameSet.Contains(name))
                        continue;
                    nameSet.Add(name);
                    if(isSpecialNumbers)
                    {
                        string type = "uint";
                        if (!string.IsNullOrEmpty(member.Type))
                            type = ENUMTYPE_REPLACE[member.Type];
                        writer.WriteLine($"public static {type} {name} = {member.Value};");
                    }
                    else
                    {
                        writer.Write($"{name} = {member.Value}");
                        writer.WriteLine(count < members.Count ? "," : string.Empty);
                    }
                    //if (enumeration.Group is null)
                    //    continue;
                    //if (enumeration.Group.Equals("SpecialNumbers", StringComparison.Ordinal))
                    //    continue;

                        //groups.RemoveAll(g => g.Name.Equals(enumeration.Group, StringComparison.Ordinal));

                        // writer.WriteLine();

                        //if (enumeration.Type is null)
                        //{
                        //    writer.WriteLine($"public enum {enumeration.Group}");
                        //}
                    //else if (enumeration.Type.Equals("bitmask", StringComparison.Ordinal))
                    //{
                    //    writer.WriteLine("[Flags]");
                    //    writer.WriteLine($"public enum {enumeration.Group} : uint");
                    //}
                    //else
                    //    throw new InvalidOperationException("Unknown enumeration type.");

                    //writer.WriteLine("{");
                    //writer.Indent++;

                    //var count = 0;
                    //List<EnumMember> members = new List<EnumMember>(enumeration);
                    //members.Sort((a, b) => a.Value.CompareTo(b.Value));
                    //HashSet<string> nameSet = new HashSet<string>();
                    //foreach (var member in members)
                    //{
                    //    count++;
                    //    var name = EnumMemberName(member.Name);
                    //    if (nameSet.Contains(name))
                    //        continue;
                    //    nameSet.Add(name);
                    //    writer.Write($"{name} = {member.Value}");
                    //    writer.WriteLine(count < enumeration.Count ? "," : string.Empty);
                    //}

                    //writer.Indent--;
                    //writer.WriteLine("}");
                }
                writer.Indent--;
                writer.WriteLine("}");
            }

            return null;
            //return groups;
        }

        //public static void GenerateGroups(GlSpec spec, IEnumerable<Spec.Group> groups, IndentedTextWriter writer)
        //{
        //    var all = spec.Enums.SelectMany(e => e).ToList();
        //    foreach (var group in groups)
        //    {
        //        writer.WriteLine($"public enum {group.Name}");
        //        writer.WriteLine("{");
        //        writer.Indent++;

        //        List<Tuple<NamedEntry, EnumMember>> list = new List<Tuple<NamedEntry, EnumMember>>();
        //        foreach (var member in group)
        //        {
        //            if (list.Exists(item => item.Item1.Name.Equals(member.Name, StringComparison.Ordinal)))
        //            {
        //                Console.WriteLine(member.Name);
        //                continue;
        //            }
        //            var m = all.Find(e => e.Name.Equals(member.Name, StringComparison.Ordinal));
        //            if (m is null)
        //            {
        //                Console.WriteLine(member.Name);
        //                continue;
        //            }
        //            list.Add(new Tuple<NamedEntry, EnumMember>(member, m));                    
        //        }

        //        foreach (var e_m in list)
        //        {
        //            writer.WriteLine($"{EnumMemberName(e_m.Item1.Name)} = {e_m.Item2.Value},");
        //        }
        //        writer.Indent--;
        //        writer.WriteLine("}");
        //        writer.WriteLine();
        //    }
            
        //}

        public static string EnumMemberName(string input)
        {
            return input;
            //var buffer = new StringBuilder();
            //foreach (var word in input.Split('_').Select(s => s.ToLower()))
            //{
            //    if (word == "gl")
            //        continue;
            //    if (WORD_REPLACE.TryGetValue(word, out var result))
            //        buffer.Append(result);
            //    else
            //    {
            //        buffer.Append(char.ToUpperInvariant(word[0]));
            //        if (word.Length > 1)
            //            buffer.Append(word.Substring(1));
            //    }
            //}
            //var str = buffer.ToString();
            //if (str[0] >= '0' && str[0] <= '9')
            //    str = "_" + str;
            //return str;
        }


        public static IEnumerable<string> GenerateCommands(GlSpec spec, Api api, Profile profile, Version version, IndentedTextWriter writer)
        {
            var buffer = new List<string>();
            HashSet<string> names = new HashSet<string>();
            foreach (var cmd in spec.GetCommands(api, version, profile))
            {
                if (names.Contains(cmd.Name))
                    continue;
                names.Add(cmd.Name);
                var import = GenerateCommand(spec, cmd, writer);
                buffer.Add(import);
                writer.WriteLine();                
            }

            foreach (var cmd in spec.GetExtensionCommands(api))
            {
                if (names.Contains(cmd.Name))
                    continue;
                names.Add(cmd.Name);
                var import = GenerateCommand(spec, cmd, writer);
                buffer.Add(import);
                writer.WriteLine();
            }

            return buffer;
        }

        private static string GenerateCommand(GlSpec spec, Command command, IndentedTextWriter writer)
        {


            var name = command.Name;
            var proto = GenerateReturnType(spec, command.Proto);
            var args = command.Select(p => GenerateParameter(spec, p)).ToList();
            var argNames = command.Select(p => GenerateParameterNames(spec, p)).ToList();

            var argString = string.Join(", ", args);

            var delegateName = $"PFN{command.Name}PROC".ToUpperInvariant();

            writer.WriteLine("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
            writer.WriteLine($"private delegate {proto} {delegateName}({argString});");
            writer.WriteLine($"private static {delegateName} _{command.Name};");
            writer.WriteLine();
            writer.WriteLine($"public static {proto} {name}({argString})");
            writer.WriteLine("{");
            writer.Indent++;

            if (!proto.Equals("void", StringComparison.Ordinal))
                writer.Write("return ");
            writer.Write($"_{command.Name}.Invoke(");
            writer.Write(string.Join(", ", argNames));
            //for (var i = 0; i < args.Count; i++)
            //{
            //    var words = args[i].Split(' ');
            //    if (words[0].Equals("out", StringComparison.Ordinal))
            //        writer.Write($"out {words.Last()}");
            //    else
            //        writer.Write(words.Last());
            //    if (i < args.Count - 1) 
            //        writer.Write(", ");
            //}
            writer.WriteLine(");");
            writer.Indent--;
            writer.WriteLine("}");

            return
                $"_{command.Name} = GetDelegateForFunctionPointer<{delegateName}>(loader,\"{command.Name}\");";
        }

        public static string GenerateParameter(GlSpec spec, Parameter param)
        {
            var name = param.Name;
            if (null == name)
                name = param.Words.Last();
            if (NAME_REPLACE.TryGetValue(name, out var value))
                name = value;

            if (param.Type is null)
            {
                if (param.IsPointer)
                {
                    return $"IntPtr {name}";
                }
                //if (param.IsConstPointer)
                //    return $"IntPtr {name}";
                //return $"out IntPtr {name}";
            }

            var type = param.Type;

            if (type.Equals("GLenum", StringComparison.Ordinal))
                type = GetEnumName(ref spec, param.Group);
            else if (type.Equals("GLbitfield", StringComparison.Ordinal))
                type = GetBitmaskName(ref spec, param.Group);
            else if (TYPE_REPLACE.TryGetValue(type, out var replace))
                type = replace;
            else
                Debug.WriteLine($"Unknown GL type: {type}");

            if (param.IsPointer)
            {
                return $"{type} " + "***********".Substring(0, param.StarCount) + $"{name}";
            }
            //{
            //    return $"out {type} {name}";
            //}
            //if (param.IsConstConstPointer)
            //{
            //    Debug.WriteLine("MEH");
            //}
            //else if (param.IsConstPointer)
            //{
            //    return $"{type}[] {name}";
            //}
            //else if (param.IsPointer)
            //{
            //    return $"out {type} {name}";
            //}

            return $"{type} {name}";
        }

        public static string GenerateParameterNames(GlSpec spec, Parameter param)
        {
            var name = param.Name;
            if (null == name)
                name = param.Words.Last();
            if (NAME_REPLACE.TryGetValue(name, out var value))
                name = value;
            return name;
        }

        private static string GenerateReturnType(GlSpec spec, Prototype proto)
        {
            if (proto.IsPointer)
                return "IntPtr";

            var type = proto.Words[0].Trim();
            if (type.Equals("void", StringComparison.Ordinal))
                return type;

            if (type.Equals("GLenum", StringComparison.Ordinal))
                return GetEnumName(ref spec, proto.Group);
            if (type.Equals("GLbitmask", StringComparison.Ordinal))
                return GetBitmaskName(ref spec, proto.Group);
            if (type.Equals("GLbitfield", StringComparison.Ordinal))
                type = GetBitmaskName(ref spec, proto.Group);

            if (TYPE_REPLACE.TryGetValue(type, out var result))
                return result;

            Debug.WriteLine($"Unknown GL type: {type}");

            return type;
        }

        private static string GetEnumName(ref GlSpec spec, string group)
        {
            if (group is null)
                return "int";
            return spec.Groups.Any(g => g.Name.Equals(group, StringComparison.Ordinal)) ? group : "int";
        }

        private static string GetBitmaskName(ref GlSpec spec, string group)
        {
            if (group is null)
                return "uint";
            return spec.Groups.Any(g => g.Name.Equals(group, StringComparison.Ordinal)) ? group : "uint";
        }
    }
}

