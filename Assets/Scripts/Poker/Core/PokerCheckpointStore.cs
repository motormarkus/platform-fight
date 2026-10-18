using System;
using System.IO;

namespace TotalFist.Poker
{
    // One replace operation commits the complete wallet + hand snapshot. The codec is injected
    // so the same durability path can be exercised without loading Unity.
    public sealed class PokerCheckpointStore<T>
    {
        readonly string path;
        readonly Func<T, string> encode;
        readonly Func<string, T> decode;
        public PokerCheckpointStore(string file, Func<T, string> serialize, Func<string, T> deserialize)
        { path = file; encode = serialize; decode = deserialize; }
        public bool Exists => File.Exists(path);
        public T Read()
        {
            if (!File.Exists(path))
            {
                if (File.Exists(path + ".bak")) throw new InvalidDataException("Primary checkpoint missing; backup retained");
                return default(T);
            }
            return decode(File.ReadAllText(path)); // Never silently reset corrupt money data.
        }
        public void Write(T snapshot)
        {
            string payload = encode(snapshot); // Serialize successfully before touching the previous checkpoint.
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string tmp = path + ".tmp";
            using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream)) { writer.Write(payload); writer.Flush(); stream.Flush(true); }
            if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp, path);
        }
    }
}
