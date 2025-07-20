using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class StableVideoDiffusion
{
    [Key(0)]
    public Nodetool.Types.ImageRef input_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int num_frames { get; set; } = 14;
    [Key(2)]
    public int num_inference_steps { get; set; } = 25;
    [Key(3)]
    public int fps { get; set; } = 7;
    [Key(4)]
    public int decode_chunk_size { get; set; } = 8;
    [Key(5)]
    public int seed { get; set; } = 42;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
