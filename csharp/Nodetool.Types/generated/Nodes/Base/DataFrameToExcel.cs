using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DataFrameToExcel
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public bool include_header { get; set; } = true;
    [Key(2)]
    public string sheet_name { get; set; } = @"Sheet1";
    [Key(3)]
    public string start_cell { get; set; } = @"A1";
    [Key(4)]
    public Nodetool.Types.Core.ExcelRef workbook { get; set; } = new Nodetool.Types.Core.ExcelRef();

    public object Process()
    {
        return default(object);
    }
}
