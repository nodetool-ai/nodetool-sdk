using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Kandinsky3Img2Img
{
    [Key(0)]
    public string prompt { get; set; } = "A photograph of the inside of a subway train. There are raccoons sitting on the seats. One of them is reading a newspaper. The window shows the city in the background.";
    [Key(1)]
    public int num_inference_steps { get; set; } = 25;
    [Key(2)]
    public double strength { get; set; } = 0.5;
    [Key(3)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(4)]
    public int seed { get; set; } = 0;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
