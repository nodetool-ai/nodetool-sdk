using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveAudio
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public Nodetool.Types.Core.FolderRef folder { get; set; } = new Nodetool.Types.Core.FolderRef();
    [Key(2)]
    public string name { get; set; } = @"%Y-%m-%d-%H-%M-%S.opus";

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
