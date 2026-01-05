using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class NumpyToAudio
{
    [Key(0)]
    public Nodetool.Types.Core.NPArray array { get; set; } = new Nodetool.Types.Core.NPArray();
    [Key(1)]
    public int channels { get; set; } = 1;
    [Key(2)]
    public int sample_rate { get; set; } = 44100;

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
