using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DownloadFile
{
    [Key(0)]
    public string url { get; set; } = @"";

    public byte[] Process()
    {
        return default(byte[]);
    }
}
