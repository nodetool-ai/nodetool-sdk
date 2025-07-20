using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Openai;

[MessagePackObject]
public class CreateImage
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object model { get; set; } = "Model.GPT_IMAGE_1";
    [Key(2)]
    public object size { get; set; } = "Size._1024x1024";
    [Key(3)]
    public object background { get; set; } = "Background.auto";
    [Key(4)]
    public object quality { get; set; } = "Quality.high";

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
