using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetMetadata
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();

    [MessagePackObject]
    public class GetMetadataOutput
    {
        [Key(0)]
        public int channels { get; set; }
        [Key(1)]
        public string format { get; set; }
        [Key(2)]
        public int height { get; set; }
        [Key(3)]
        public string mode { get; set; }
        [Key(4)]
        public int width { get; set; }
    }

    public GetMetadataOutput Process()
    {
        return new GetMetadataOutput();
    }
}
