using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveDataframe
{
    [Key(0)]
    public Nodetool.Types.DataframeRef df { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();
    [Key(2)]
    public string name { get; set; } = "output.csv";

    public Nodetool.Types.DataframeRef Process()
    {
        return default(Nodetool.Types.DataframeRef);
    }
}
