using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveDataframe
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef df { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();
    [Key(2)]
    public string name { get; set; } = @"output.csv";

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
