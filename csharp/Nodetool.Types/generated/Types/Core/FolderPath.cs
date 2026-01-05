using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class FolderPath
{
    [Key(0)]
    public string path { get; set; } = @"";
    [Key(1)]
    public object type { get; set; } = @"folder_path";
}
