using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DocumentFileInput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public string value { get; set; } = @"";

    [MessagePackObject]
    public class DocumentFileInputOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.DocumentRef document { get; set; }
        [Key(1)]
        public string path { get; set; }
    }

    public DocumentFileInputOutput Process()
    {
        return new DocumentFileInputOutput();
    }
}
