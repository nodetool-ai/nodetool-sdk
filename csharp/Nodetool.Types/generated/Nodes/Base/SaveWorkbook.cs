using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SaveWorkbook
{
    [Key(0)]
    public string filename { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.FilePath folder { get; set; } = new Nodetool.Types.Core.FilePath();
    [Key(2)]
    public Nodetool.Types.Core.ExcelRef workbook { get; set; } = new Nodetool.Types.Core.ExcelRef();

    public void Process()
    {
    }
}
