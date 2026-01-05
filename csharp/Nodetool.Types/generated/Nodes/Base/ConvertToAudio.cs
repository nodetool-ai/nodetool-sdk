using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ConvertToAudio
{
    [Key(0)]
    public int sample_rate { get; set; } = 44100;
    [Key(1)]
    public Nodetool.Types.Core.NPArray values { get; set; } = new Nodetool.Types.Core.NPArray();

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
