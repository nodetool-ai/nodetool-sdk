using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateWorkbook
{
    [Key(0)]
    public string sheet_name { get; set; } = @"Sheet1";

    public Nodetool.Types.Core.ExcelRef Process()
    {
        return default(Nodetool.Types.Core.ExcelRef);
    }
}
