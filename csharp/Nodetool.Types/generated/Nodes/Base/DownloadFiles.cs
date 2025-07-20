using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DownloadFiles
{
    [Key(0)]
    public object urls { get; set; } = new List<object>();
    [Key(1)]
    public Nodetool.Types.FilePath output_folder { get; set; } = new Nodetool.Types.FilePath();
    [Key(2)]
    public int max_concurrent_downloads { get; set; } = 5;

    [MessagePackObject]
    public class DownloadFilesOutput
    {
        [Key(0)]
        public object success { get; set; }
        [Key(1)]
        public object failed { get; set; }
    }

    public DownloadFilesOutput Process()
    {
        return new DownloadFilesOutput();
    }
}
