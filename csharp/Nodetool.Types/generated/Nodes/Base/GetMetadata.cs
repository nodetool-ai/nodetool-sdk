using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Image;

[MessagePackObject]
public class GetMetadata
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();

    [MessagePackObject]
    public class GetMetadataOutput
    {
        [Key(0)]
        public string format { get; set; }
        [Key(1)]
        public string mode { get; set; }
        [Key(2)]
        public int width { get; set; }
        [Key(3)]
        public int height { get; set; }
        [Key(4)]
        public int channels { get; set; }
    }

    public GetMetadataOutput Process()
    {
        // Implementation would be generated based on node logic
        return new GetMetadataOutput();
    }
}
