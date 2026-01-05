using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LoadImageFolder
{
    [Key(0)]
    public object extensions { get; set; } = new();
    [Key(1)]
    public string folder { get; set; } = @"";
    [Key(2)]
    public bool include_subdirectories { get; set; } = false;
    [Key(3)]
    public string pattern { get; set; } = @"";

    [MessagePackObject]
    public class LoadImageFolderOutput
    {
        [Key(0)]
        public Nodetool.Types.Core.ImageRef image { get; set; }
        [Key(1)]
        public string path { get; set; }
    }

    public LoadImageFolderOutput Process()
    {
        return new LoadImageFolderOutput();
    }
}
