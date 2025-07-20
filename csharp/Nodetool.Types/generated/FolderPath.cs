using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class FolderPath
{
    [Key(0)]
    public object type { get; set; } = "folder_path";
    [Key(1)]
    public string path { get; set; } = "";
}
