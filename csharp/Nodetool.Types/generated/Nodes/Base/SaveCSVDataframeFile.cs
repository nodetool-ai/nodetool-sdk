using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveCSVDataframeFile
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public string filename { get; set; } = @"";
    [Key(2)]
    public string folder { get; set; } = @"";

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
