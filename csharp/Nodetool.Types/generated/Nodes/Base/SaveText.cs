using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveText
{
    [Key(0)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();
    [Key(1)]
    public string name { get; set; } = @"%Y-%m-%d-%H-%M-%S.txt";
    [Key(2)]
    public string text { get; set; } = @"";

    public Nodetool.Types.Core.TextRef Process()
    {
        return default(Nodetool.Types.Core.TextRef);
    }
}
