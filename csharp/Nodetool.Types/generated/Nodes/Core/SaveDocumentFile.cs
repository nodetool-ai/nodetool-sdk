using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class SaveDocumentFile
{
    [Key(0)]
    public Nodetool.Types.DocumentRef document { get; set; } = new Nodetool.Types.DocumentRef();
    [Key(1)]
    public Nodetool.Types.FolderPath folder { get; set; } = new Nodetool.Types.FolderPath();
    [Key(2)]
    public string filename { get; set; } = "";

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
