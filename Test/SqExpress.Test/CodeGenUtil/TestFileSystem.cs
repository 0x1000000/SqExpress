#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using SqExpress.CodeGenUtil;

namespace SqExpress.Test.CodeGenUtil
{
    public class TestFileSystem : IFileSystem
    {
        private readonly Dictionary<string, List<string>> _directories = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public bool DirectoryExists(string path)
        {
            return this._directories.ContainsKey(path);
        }

        public bool FileExists(string path)
        {
            return this._files.ContainsKey(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern, SearchOption searchOption)
        {
            if (this._directories.TryGetValue(path, out var list))
            {
                return list;
            }
            throw new Exception("Directory does not exists: " + path);
        }

        public string ReadAllText(string path)
        {
            if (this._files.TryGetValue(path, out var list))
            {
                return list;
            }
            throw new Exception("File does not exists: " + path);
        }

        public void AddFile(string path, string content)
        {
            var dir = Path.GetDirectoryName(path) ?? string.Empty;

            if (!this._directories.TryGetValue(dir, out var list))
            {
                list = new List<string>();
                this._directories.Add(dir, list);
            }
            list.Add(path);

            if(!this._files.TryAdd(path, content))
            {
                throw new Exception("File already exists: " + path);
            }
        }
    }
}
#endif