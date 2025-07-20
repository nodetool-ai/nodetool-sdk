using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class ExtractTarFile
{
    [Key(0)]
    public Nodetool.Types.FilePath tar_path { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public Nodetool.Types.FilePath output_folder { get; set; } = new Nodetool.Types.FilePath();

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
