using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ImageDownloader
{
    [Key(0)]
    public object images { get; set; } = new List<object>();
    [Key(1)]
    public string base_url { get; set; } = "";
    [Key(2)]
    public int max_concurrent_downloads { get; set; } = 10;

    [MessagePackObject]
    public class ImageDownloaderOutput
    {
        [Key(0)]
        public object images { get; set; }
        [Key(1)]
        public object failed_urls { get; set; }
    }

    public ImageDownloaderOutput Process()
    {
        // Implementation would be generated based on node logic
        return new ImageDownloaderOutput();
    }
}
