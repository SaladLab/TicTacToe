#if UNITY_EDITOR_WIN

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SyntaxTree.VisualStudio.Unity.Bridge;
using UnityEditor;

[InitializeOnLoad]
public class CodeAnalysisVsHook
{
    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }

    // NOTE: Good to have
    // - automatic nuget package download
    // - stylecop file or directory exclude list

    static CodeAnalysisVsHook()
    {
        ProjectFilesGenerator.ProjectFileGeneration += (name, content) =>
        {
            var rulesetPath = GetRulesetFile();
            if (string.IsNullOrEmpty(rulesetPath))
                return content;

            var getStyleCopAnalyzersPath = GetStyleCopAnalyzersPath();
            if (string.IsNullOrEmpty(getStyleCopAnalyzersPath))
                return content;

            // Insert a ruleset file and StyleCop.Analyzers into a project file

            var document = XDocument.Parse(content);

            var ns = document.Root.Name.Namespace;

            var propertyGroup = document.Root.Descendants(ns + "PropertyGroup").FirstOrDefault();
            if (propertyGroup != null)
            {
                propertyGroup.Add(new XElement(ns + "CodeAnalysisRuleSet", rulesetPath));
            }

            var itemGroup = document.Root.Descendants(ns + "ItemGroup").LastOrDefault();
            if (itemGroup != null)
            {
                var newItemGroup = new XElement(ns + "ItemGroup");
                foreach (var file in Directory.GetFiles(getStyleCopAnalyzersPath + @"\analyzers\dotnet\cs", "*.dll"))
                {
                    newItemGroup.Add(new XElement(ns + "Analyzer", new XAttribute("Include", file)));
                }
                itemGroup.AddAfterSelf(newItemGroup);
            }

            var str = new Utf8StringWriter();
            document.Save(str);
            return str.ToString();
        };
    }

    private static string GetRulesetFile()
    {
        // Find *.ruleset in traversing parent directories.

        var dir = Directory.GetCurrentDirectory();
        try
        {
            while (true)
            {
                var files = Directory.GetFiles(dir, "*.ruleset");
                if (files.Length > 0)
                    return files[0];

                dir = Path.GetDirectoryName(dir);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string GetStyleCopAnalyzersPath()
    {
        // Find /packages/StyleCop.Analyzers* in traversing parent directories.

        var dir = Directory.GetCurrentDirectory();
        try
        {
            while (true)
            {
                var packagesPath = Path.Combine(dir, "packages");
                if (Directory.Exists(packagesPath))
                {
                    var dirs = Directory.GetDirectories(packagesPath, "StyleCop.Analyzers*");
                    if (dirs.Length > 0)
                        return dirs[0];
                }

                dir = Path.GetDirectoryName(dir);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }
}

#endif
