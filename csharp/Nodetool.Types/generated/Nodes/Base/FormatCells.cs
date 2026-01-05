using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FormatCells
{
    [Key(0)]
    public string background_color { get; set; } = @"FFFF00";
    [Key(1)]
    public bool bold { get; set; } = false;
    [Key(2)]
    public string cell_range { get; set; } = @"A1:B10";
    [Key(3)]
    public string sheet_name { get; set; } = @"Sheet1";
    [Key(4)]
    public string text_color { get; set; } = @"000000";
    [Key(5)]
    public Nodetool.Types.Core.ExcelRef workbook { get; set; } = new Nodetool.Types.Core.ExcelRef();

    public object Process()
    {
        return default(object);
    }
}
