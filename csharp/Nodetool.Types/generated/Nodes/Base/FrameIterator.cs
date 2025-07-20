using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Video;

[MessagePackObject]
public class FrameIterator
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public int start { get; set; } = 0;
    [Key(2)]
    public int end { get; set; } = -1;

    [MessagePackObject]
    public class FrameIteratorOutput
    {
        [Key(0)]
        public Nodetool.Types.ImageRef frame { get; set; }
        [Key(1)]
        public int index { get; set; }
        [Key(2)]
        public double fps { get; set; }
        [Key(3)]
        public Nodetool.Types.Event event { get; set; }
    }

    public FrameIteratorOutput Process()
    {
        // Implementation would be generated based on node logic
        return new FrameIteratorOutput();
    }
}
