using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class SVGGenerator
{
    [Key(0)]
    public Nodetool.Types.LanguageModel model { get; set; } = new Nodetool.Types.LanguageModel();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(3)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(4)]
    public int max_tokens { get; set; } = 8192;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
