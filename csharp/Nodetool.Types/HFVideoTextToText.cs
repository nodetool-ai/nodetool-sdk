using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class HFVideoTextToText
{
    [Key(0)]
    public object type { get; set; } = "hf.video_text_to_text";
    [Key(1)]
    public string repo_id { get; set; } = "";
    [Key(2)]
    public object path { get; set; } = null;
    [Key(3)]
    public object variant { get; set; } = null;
    [Key(4)]
    public object allow_patterns { get; set; } = null;
    [Key(5)]
    public object ignore_patterns { get; set; } = null;
}
