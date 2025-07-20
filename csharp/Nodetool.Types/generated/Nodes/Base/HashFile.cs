using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HashFile
{
    [Key(0)]
    public Nodetool.Types.FilePath file { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public string algorithm { get; set; } = "md5";
    [Key(2)]
    public int chunk_size { get; set; } = 8192;

    public string Process()
    {
        return default(string);
    }
}
