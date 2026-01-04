using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DownloadDataframe
{
    [Key(0)]
    public Nodetool.Types.Core.RecordType columns { get; set; } = new Nodetool.Types.Core.RecordType();
    [Key(1)]
    public string delimiter { get; set; } = @",";
    [Key(2)]
    public string encoding { get; set; } = @"utf-8";
    [Key(3)]
    public object file_format { get; set; } = @"csv";
    [Key(4)]
    public string url { get; set; } = @"";

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
