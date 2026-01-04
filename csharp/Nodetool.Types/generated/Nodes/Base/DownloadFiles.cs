using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DownloadFiles
{
    [Key(0)]
    public int max_concurrent_downloads { get; set; } = 5;
    [Key(1)]
    public string output_folder { get; set; } = @"downloads";
    [Key(2)]
    public object urls { get; set; } = new();

    [MessagePackObject]
    public class DownloadFilesOutput
    {
        [Key(0)]
        public object failed { get; set; }
        [Key(1)]
        public object success { get; set; }
    }

    public DownloadFilesOutput Process()
    {
        return new DownloadFilesOutput();
    }
}
