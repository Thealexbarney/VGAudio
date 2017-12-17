#if NET461
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;

namespace Build
{
    public static class Trim
    {
        private static readonly string[] TrimNamespaces = { "VGAudio.Utilities", "VGAudio.Utilities.Riff", "VGAudio.Codecs" };
        private static readonly bool Verbose = false;

        public static void TrimSolution(Context context)
        {
            MSBuildWorkspace ws = OpenSolution(context.TrimSolution.FullPath);
            TrimProject(ws, "VGAudio");
        }

        private static MSBuildWorkspace OpenSolution(string path)
        {
            MSBuildWorkspace workspace =
                MSBuildWorkspace.Create(new Dictionary<string, string> { ["TargetFramework"] = "netstandard1.1" });
            // ReSharper disable once UnusedVariable
            Solution solution = workspace.OpenSolutionAsync(path).Result;
            return workspace;
        }

        private static void TrimProject(MSBuildWorkspace ws, string projectName)
        {
            int changes = 1;
            int i = 0;
            while (changes > 0)
            {
                changes = 0;
                Solution solution = ws.CurrentSolution;
                Project project = solution.Projects.First(x => x.Name == projectName);
                Compilation c = project.GetCompilationAsync().Result;
                if (Verbose) Console.WriteLine($"Pass {i++}");

                var referenceNodes = FindReferences(solution, c);
                var callerNodes = FindMethods(solution, c);
                SyntaxNode[] nodes = referenceNodes.Concat(callerNodes).Distinct().ToArray();
                SyntaxNode[] trimmedNodes = RemoveChildNodes(nodes);

                solution = RunTrimPass(project, trimmedNodes, ref changes);
                ws.TryApplyChanges(solution);
            }
        }

        private static Solution RunTrimPass(Project project, SyntaxNode[] removeNodes, ref int changes)
        {
            Solution solution = project.Solution;

            foreach (Document document in project.Documents)
            {
                SyntaxNode root = document.GetSyntaxRootAsync().Result;
                SyntaxNode[] documentNodes = root.DescendantNodes().ToArray();
                IEnumerable<SyntaxNode> nodes = removeNodes.Where(x => documentNodes.Contains(x));
                DocumentEditor editor = DocumentEditor.CreateAsync(document).Result;

                foreach (var node in nodes)
                {
                    if (Verbose) Console.WriteLine(document.Name);
                    editor.RemoveNode(node);
                    changes++;
                }

                RemoveEmptyNamespaceDirectives(editor, documentNodes, ref changes);

                if (IsCompilationUnitEmpty(root as CompilationUnitSyntax))
                {
                    if (Verbose) Console.WriteLine($"Remove {document.Name}");
                    File.Delete(document.FilePath);
                }

                solution = solution.WithDocumentSyntaxRoot(document.Id, editor.GetChangedRoot());
            }

            return solution;
        }

        private static bool IsCompilationUnitEmpty(CompilationUnitSyntax node)
        {
            return node != null && !node.Members.Any() && !node.AttributeLists.Any();
        }

        private static void RemoveEmptyNamespaceDirectives(DocumentEditor editor, SyntaxNode[] nodes, ref int changes)
        {
            var emptyNsNodes = nodes.OfType<NamespaceDeclarationSyntax>().Where(x => !x.Members.Any());
            foreach (NamespaceDeclarationSyntax node in emptyNsNodes)
            {
                if (Verbose) Console.WriteLine(editor.OriginalDocument.Name);
                editor.RemoveNode(node);
                changes++;
            }
        }

        private static IEnumerable<SyntaxNode> FindReferences(Solution solution, Compilation c)
        {
            return c.GetSymbolsWithName(x => true)
                .Where(x => TrimNamespaces.Contains(x.ContainingNamespace.ToString()))
                .Where(x => !SymbolFinder.FindReferencesAsync(x, solution).Result.SelectMany(y => y.Locations).Any())
                .Where(x => (x as INamedTypeSymbol)?.MightContainExtensionMethods != true)
                .SelectMany(x => x.DeclaringSyntaxReferences)
                .Select(x => x.GetSyntax());
        }

        private static IEnumerable<SyntaxNode> FindMethods(Solution solution, Compilation c)
        {
            return c.GetSymbolsWithName(x => true)
                .OfType<IMethodSymbol>()
                .Where(x => TrimNamespaces.Contains(x.ContainingNamespace.ToString()))
                .Where(x => !SymbolFinder.FindCallersAsync(x, solution).Result
                    .Where(z => !Equals(z.CalledSymbol, z.CallingSymbol)).SelectMany(y => y.Locations).Any())
                .SelectMany(x => x.DeclaringSyntaxReferences)
                .Select(x => x.GetSyntax());
        }

        private static SyntaxNode[] RemoveChildNodes(SyntaxNode[] nodes)
        {
            var toRemove = new HashSet<SyntaxNode>();
            var nodesHash = nodes.ToImmutableHashSet();

            foreach (SyntaxNode node in nodes)
            {
                foreach (SyntaxNode parent in GetNodeParents(node))
                {
                    if (nodesHash.Contains(parent))
                    {
                        toRemove.Add(node);
                        break;
                    }
                }
            }
            return nodes.Where(x => !toRemove.Contains(x)).ToArray();
        }

        private static IEnumerable<SyntaxNode> GetNodeParents(SyntaxNode node)
        {
            while (node.Parent != null)
            {
                SyntaxNode parent = node.Parent;
                node = parent;
                yield return parent;
            }
        }
    }
}
#endif