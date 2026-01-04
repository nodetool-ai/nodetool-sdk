using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FrameIterator
{
    [Key(0)]
    public int end { get; set; } = -1;
    [Key(1)]
    public int start { get; set; } = 0;
    [Key(2)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    [MessagePackObject]
    public class FrameIteratorOutput
    {
        [Key(0)]
        public double fps { get; set; }
        [Key(1)]
        public Nodetool.Types.Core.ImageRef frame { get; set; }
        [Key(2)]
        public int index { get; set; }
    }

    public FrameIteratorOutput Process()
    {
        return new FrameIteratorOutput();
    }
}
