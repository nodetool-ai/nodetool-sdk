using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class WebsiteContentExtractor
{
    [Key(0)]
    public string html_content { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
