using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AutoFitColumns
{
    [Key(0)]
    public string sheet_name { get; set; } = @"Sheet1";
    [Key(1)]
    public Nodetool.Types.Core.ExcelRef workbook { get; set; } = new Nodetool.Types.Core.ExcelRef();

    public object Process()
    {
        return default(object);
    }
}
