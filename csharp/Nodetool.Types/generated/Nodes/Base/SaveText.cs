using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Text;

[MessagePackObject]
public class SaveText
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.FolderRef folder { get; set; } = new Nodetool.Types.FolderRef();
    [Key(2)]
    public string name { get; set; } = "%Y-%m-%d-%H-%M-%S.txt";

    public Nodetool.Types.TextRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.TextRef);
    }
}
