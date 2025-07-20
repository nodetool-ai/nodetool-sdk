using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class CreateTarFile
{
    [Key(0)]
    public Nodetool.Types.FilePath source_folder { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public Nodetool.Types.FilePath tar_path { get; set; } = new Nodetool.Types.FilePath();
    [Key(2)]
    public bool gzip { get; set; } = false;

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
