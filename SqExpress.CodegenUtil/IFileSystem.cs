using System.Collections.Generic;
using System.IO;

namespace SqExpress.CodeGenUtil
{
    internal interface IFileSystem
    {
        bool DirectoryExists(string path);

        bool FileExists(string path);

        IEnumerable<string> EnumerateFiles(string path, string pattern, SearchOption searchOption);

        string ReadAllText(string path);
    }

    internal class DefaultFileSystem : IFileSystem
    {
        public static readonly DefaultFileSystem Instance = new DefaultFileSystem();

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(path, pattern, searchOption);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}