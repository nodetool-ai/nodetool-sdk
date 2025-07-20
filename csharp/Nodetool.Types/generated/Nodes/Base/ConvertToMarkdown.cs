using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ConvertToMarkdown
{
    [Key(0)]
    public Nodetool.Types.DocumentRef document { get; set; } = new Nodetool.Types.DocumentRef();

    public string Process()
    {
        return default(string);
    }
}
