﻿using System;
using System.Collections.Generic;
using System.IO;
using Docu.Documentation;
using Docu.Events;
using Docu.IO;
using Docu.Output;
using Docu.Parsing;
using Docu.Parsing.Comments;

namespace Docu
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var arguments = new List<string>(args);

            Console.WriteLine("-------------------------------");
            Console.WriteLine(" docu: simple docs done simply ");
            Console.WriteLine("-------------------------------");
            Console.WriteLine();

            if (arguments.Count == 0 || arguments.Contains("--help"))
            {
                Console.WriteLine("usuage: docu pattern.dll [pattern.dll ...] [pattern.xml ...]");
                Console.WriteLine();
                Console.WriteLine(" * One or more dll matching patterns can be specified,");
                Console.WriteLine("   e.g. MyProject*.dll or MyProject.dll");
                Console.WriteLine();
                Console.WriteLine(" * Xml file names can be inferred from the dll patterns");
                Console.WriteLine("   or can be explicitly named.");
                Console.WriteLine();
                Console.WriteLine("Switches:");
                Console.WriteLine("  --help          Shows this message");
                Console.WriteLine("  --output=value  Sets the output path to value");
                return;
            }

            var eventAggregator = new EventAggregator();
            eventAggregator.GetEvent<WarningEvent>().Subscribe(message => Console.WriteLine("WARNING: " + message));
            eventAggregator.GetEvent<BadFileEvent>().Subscribe(path => Console.WriteLine("The requested file is in a bad format and could not be loaded: '" + path + "'"));

            var commentParser = new CommentParser(new ICommentNodeParser[]
                {
                    new InlineCodeCommentParser(),
                    new InlineListCommentParser(),
                    new InlineTextCommentParser(),
                    new MultilineCodeCommentParser(),
                    new ParagraphCommentParser(),
                    new ParameterReferenceParser(),
                    new SeeCodeCommentParser(),
                });

            var documentModel = new DocumentModel(commentParser, eventAggregator);
            var pageWriter = new BulkPageWriter(new PageWriter(new HtmlGenerator(), new FileSystemOutputWriter(), new PatternTemplateResolver()));
            var generator = new DocumentationGenerator(new AssemblyLoader(), new XmlLoader(), new AssemblyXmlParser(documentModel), pageWriter, new UntransformableResourceManager(), eventAggregator);

            for (int i = arguments.Count - 1; i >= 0; i--)
            {
                if (arguments[i].StartsWith("--output="))
                {
                    generator.SetOutputPath(arguments[i].Substring(9).TrimEnd('\\'));
                    arguments.RemoveAt(i);
                    continue;
                }
                if (arguments[i].StartsWith("--templates="))
                {
                    generator.SetTemplatePath(arguments[i].Substring(12).TrimEnd('\\'));
                    arguments.RemoveAt(i);
                }
            }

            string[] assemblies = GetAssembliesFromArgs(arguments);
            string[] xmls = GetXmlsFromArgs(arguments, assemblies);

            if (Verify(arguments, assemblies, xmls))
            {
                Console.WriteLine("Starting documentation generation");

                generator.SetAssemblies(assemblies);
                generator.SetXmlFiles(xmls);
                generator.Generate();

                Console.WriteLine();
                Console.WriteLine("Generation complete");
            }
        }

        static string GetExpectedXmlFileForAssembly(string assembly)
        {
            string extension = Path.GetExtension(assembly);
            return assembly.Substring(0, assembly.Length - extension.Length) + ".xml";
        }

        static bool IsAssemblyArgument(string argument)
        {
            string fileExtension = Path.GetExtension(argument);
            return fileExtension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase)
                || fileExtension.Equals(".exe", StringComparison.InvariantCultureIgnoreCase);
        }

        static string[] GetAssembliesFromArgs(IEnumerable<string> args)
        {
            var assemblies = new List<string>();

            foreach (string arg in args)
            {
                if (IsAssemblyArgument(arg))
                {
                    assemblies.AddRange(GetFiles(arg));
                }
            }

            return assemblies.ToArray();
        }

        static IEnumerable<string> GetFiles(string path)
        {
            if (path.Contains("*") || path.Contains("?"))
            {
                string dir = Environment.CurrentDirectory;
                string filename = Path.GetFileName(path);

                if (path.Contains("\\"))
                {
                    dir = Path.GetDirectoryName(path);
                }

                foreach (string file in Directory.GetFiles(dir, filename))
                {
                    yield return file.Replace(Environment.CurrentDirectory + "\\", string.Empty);
                }
            }
            else
            {
                yield return path;
            }
        }

        static string[] GetXmlsFromArgs(IEnumerable<string> args, IEnumerable<string> assemblies)
        {
            var xmls = new List<string>();

            foreach (string arg in args)
            {
                if (arg.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                {
                    xmls.AddRange(GetFiles(arg));
                }
            }

            if (xmls.Count == 0)
            {
                // none specified, try to find some
                foreach (string assembly in assemblies)
                {
                    string name = GetExpectedXmlFileForAssembly(assembly);

                    foreach (string file in GetFiles(name))
                    {
                        if (File.Exists(file))
                        {
                            xmls.Add(file);
                        }
                    }
                }
            }

            return xmls.ToArray();
        }

        static bool Verify(IEnumerable<string> arguments, string[] assemblies, string[] xmls)
        {
            foreach (string argument in arguments)
            {
                if (IsAssemblyArgument(argument)
                    || Path.GetExtension(argument).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Console.WriteLine("Invalid argument '" + argument + "': what am I supposed to do with this?");
                Console.WriteLine();
                Console.WriteLine("Use --help to see usage and switches");
                return false;
            }

            if (assemblies.Length == 0)
            {
                Console.WriteLine("No Assemblies specified: please give me some assemblies!");
                return false;
            }

            if (xmls.Length == 0)
            {
                Console.WriteLine("No XML documents found: none were found for the assembly names you gave,");
                Console.WriteLine("explicitly specify them if their names differ from their associated assembly.");
                return false;
            }

            foreach (string assembly in assemblies)
            {
                if (!File.Exists(assembly))
                {
                    Console.WriteLine("Assembly not found '" + assembly + "': cannot continue.");
                    return false;
                }
            }

            foreach (string xml in xmls)
            {
                if (!File.Exists(xml))
                {
                    Console.WriteLine("XML file not found '" + xml + "': cannot continue.");
                    return false;
                }
            }

            return true;
        }
    }
}