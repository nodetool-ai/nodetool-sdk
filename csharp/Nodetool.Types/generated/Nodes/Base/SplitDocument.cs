using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitDocument
{
    [Key(0)]
    public int buffer_size { get; set; } = 1;
    [Key(1)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(2)]
    public Nodetool.Types.Core.LanguageModel embed_model { get; set; } = new Nodetool.Types.Core.LanguageModel();
    [Key(3)]
    public int threshold { get; set; } = 95;
}
