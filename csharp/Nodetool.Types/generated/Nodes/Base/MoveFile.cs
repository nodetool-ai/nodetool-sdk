using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MoveFile
{
    [Key(0)]
    public Nodetool.Types.FilePath source_path { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public Nodetool.Types.FilePath destination_path { get; set; } = new Nodetool.Types.FilePath();

    public void Process()
    {
    }
}
