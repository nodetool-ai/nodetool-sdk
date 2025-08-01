using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleImages
{
    [Key(0)]
    public string keyword { get; set; } = "";
    [Key(1)]
    public string image_url { get; set; } = "";
    [Key(2)]
    public int num_results { get; set; } = 20;

    [MessagePackObject]
    public class GoogleImagesOutput
    {
        [Key(0)]
        public object results { get; set; }
        [Key(1)]
        public object images { get; set; }
    }

    public GoogleImagesOutput Process()
    {
        return new GoogleImagesOutput();
    }
}
