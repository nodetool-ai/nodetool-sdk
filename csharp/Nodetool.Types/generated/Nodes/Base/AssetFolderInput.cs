using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AssetFolderInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.FolderRef value { get; set; } = new Nodetool.Types.Core.FolderRef();

    public Nodetool.Types.Core.FolderRef Process()
    {
        return default(Nodetool.Types.Core.FolderRef);
    }
}
