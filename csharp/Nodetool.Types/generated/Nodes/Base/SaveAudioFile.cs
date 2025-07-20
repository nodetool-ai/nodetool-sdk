using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveAudioFile
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public Nodetool.Types.FolderPath folder { get; set; } = new Nodetool.Types.FolderPath();
    [Key(2)]
    public string filename { get; set; } = "";

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
