using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGen.Shared;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil
{
    internal static class TableOutputCleaner
    {
        public static IReadOnlyList<string> Clean(
            string outputDirectory,
            IReadOnlyDictionary<TableRef, TableGenerationLayoutEntry> currentLayout)
        {
            var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var expectedPaths = currentLayout.ToDictionary(
                static pair => pair.Key,
                static pair => Path.GetFullPath(pair.Value.FilePath),
                EqualityComparer<TableRef>.Default);
            var obsolete = CodeGenLegacySqModelSupport
                .FindTableDescriptorLocations(outputDirectory, DefaultFileSystem.Instance)
                .Where(location =>
                    !expectedPaths.TryGetValue(location.TableRef, out var expectedPath) ||
                    !pathComparer.Equals(Path.GetFullPath(location.FilePath), expectedPath))
                .GroupBy(static location => location.FilePath, pathComparer)
                .ToList();

            var removedFiles = new List<string>();
            var directoriesToCheck = new HashSet<string>(pathComparer);
            foreach (var fileGroup in obsolete)
            {
                var filePath = fileGroup.Key;
                var root = fileGroup.First().ClassDeclaration.SyntaxTree.GetCompilationUnitRoot();
                var updatedRoot = root.RemoveNodes(
                    fileGroup.Select(static location => location.ClassDeclaration).Distinct(),
                    SyntaxRemoveOptions.KeepExteriorTrivia);
                if (updatedRoot == null || !ContainsTypeDeclaration(updatedRoot))
                {
                    File.Delete(filePath);
                    removedFiles.Add(filePath);
                    var directory = Path.GetDirectoryName(filePath);
                    if (directory != null)
                    {
                        directoriesToCheck.Add(directory);
                    }
                }
                else
                {
                    File.WriteAllText(filePath, updatedRoot.ToFullString());
                }
            }

            RemoveEmptyDirectories(outputDirectory, directoriesToCheck, pathComparer);
            return removedFiles;
        }

        private static bool ContainsTypeDeclaration(CompilationUnitSyntax root)
            => root.DescendantNodes().Any(static node =>
                node is BaseTypeDeclarationSyntax || node is DelegateDeclarationSyntax);

        private static void RemoveEmptyDirectories(
            string outputDirectory,
            IEnumerable<string> directories,
            StringComparer pathComparer)
        {
            var outputPath = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var initialDirectory in directories.OrderByDescending(static path => path.Length))
            {
                var directory = Path.GetFullPath(initialDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                while (!pathComparer.Equals(directory, outputPath) &&
                       Directory.Exists(directory) &&
                       !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    var parent = Path.GetDirectoryName(directory);
                    if (parent == null)
                    {
                        break;
                    }

                    directory = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }
        }
    }
}
