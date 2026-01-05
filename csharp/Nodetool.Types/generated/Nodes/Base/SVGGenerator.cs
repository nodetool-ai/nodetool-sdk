using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SVGGenerator
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public int max_tokens { get; set; } = 8192;
    [Key(3)]
    public Nodetool.Types.Core.LanguageModel model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(4)]
    public string prompt { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
