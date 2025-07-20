using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class FilePath
{
    [Key(0)]
    public object type { get; set; } = "file_path";
    [Key(1)]
    public string path { get; set; } = "";
}
