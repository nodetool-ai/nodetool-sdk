using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExcelToDataFrame
{
    [Key(0)]
    public bool has_header { get; set; } = true;
    [Key(1)]
    public string sheet_name { get; set; } = @"Sheet1";
    [Key(2)]
    public Nodetool.Types.Core.ExcelRef workbook { get; set; } = new Nodetool.Types.Core.ExcelRef();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
