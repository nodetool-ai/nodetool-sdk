using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class HtmlToText
{
    [Key(0)]
    public string html { get; set; } = "";
    [Key(1)]
    public string base_url { get; set; } = "";
    [Key(2)]
    public int body_width { get; set; } = 1000;
    [Key(3)]
    public bool ignore_images { get; set; } = true;
    [Key(4)]
    public bool ignore_mailto_links { get; set; } = true;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
