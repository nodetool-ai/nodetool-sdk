using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractMetadata
{
    [Key(0)]
    public string html { get; set; } = @"";

    [MessagePackObject]
    public class ExtractMetadataOutput
    {
        [Key(0)]
        public string description { get; set; }
        [Key(1)]
        public string keywords { get; set; }
        [Key(2)]
        public string title { get; set; }
    }

    public ExtractMetadataOutput Process()
    {
        return new ExtractMetadataOutput();
    }
}
