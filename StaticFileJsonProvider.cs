using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Guineapig.AspNetCore
{
    public class FileEntry
    {
        public string Path { get; set; }
        public string Content { get; set; }
        public byte[] ToByteArray() =>
            Content.StartsWith("data:") && Content.Contains(";base64,") ?
            Convert.FromBase64String(Content.Substring(Content.IndexOf(',') + 1)) :
            System.Text.Encoding.UTF8.GetBytes(Content);
        public Stream GetStream() => new MemoryStream(ToByteArray());
        public static DateTime LastModified = DateTime.UtcNow;
    }

    public class StaticFileJsonProvider : IFileProvider
    {
        FileEntry[] files;
        public StaticFileJsonProvider(string jsonPath)
        {
            FileEntry.LastModified = File.GetLastWriteTimeUtc(jsonPath);
            files = JsonSerializer.Deserialize<FileEntry[]>(File.ReadAllText(jsonPath), new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true
            })!;
        }
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            Debug.WriteLine($"GetDirectoryContents - {subpath}");
            return new DirectoryContents(files.Where(o => o.Path.StartsWith(subpath)));
        }
        public IFileInfo GetFileInfo(string subpath)
        {
            Debug.WriteLine($"GetFileInfo - {subpath}");
            var find = files.FirstOrDefault(o => o.Path == subpath);
            if (find != null) return new FileInfo(find);
            return new NotFoundFileInfo(subpath);
        }
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    public class DirectoryContents : IDirectoryContents
    {
        IFileInfo[] files;
        public DirectoryContents(IEnumerable<FileEntry> files)
        {
            this.files = files.Select(o => new FileInfo(o)).ToArray();
        }
        public bool Exists => true;
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            foreach (var f in files) yield return f;
        }
        IEnumerator IEnumerable.GetEnumerator() => files.GetEnumerator();
    }
    public class FileInfo : IFileInfo
    {
        Stream stream;
        public FileInfo(FileEntry file)
        {
            stream = file.GetStream();
            Name = Path.GetFileName(file.Path);
        }
        public bool Exists => true;
        public bool IsDirectory => false;
        public DateTimeOffset LastModified => FileEntry.LastModified;
        public long Length => stream.Length;
        public string Name { get; private set; }
        public string PhysicalPath => null!;
        public Stream CreateReadStream() => stream;
    }
}