using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DownloadDataframe
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public object file_format { get; set; } = "FileFormat.CSV";
    [Key(2)]
    public Nodetool.Types.RecordType columns { get; set; } = new Nodetool.Types.RecordType();
    [Key(3)]
    public string encoding { get; set; } = "utf-8";
    [Key(4)]
    public string delimiter { get; set; } = ",";

    public Nodetool.Types.DataframeRef Process()
    {
        return default(Nodetool.Types.DataframeRef);
    }
}
