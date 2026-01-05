using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleShopping
{
    [Key(0)]
    public string condition { get; set; } = @"";
    [Key(1)]
    public string country { get; set; } = @"us";
    [Key(2)]
    public int max_price { get; set; } = 0;
    [Key(3)]
    public int min_price { get; set; } = 0;
    [Key(4)]
    public int num_results { get; set; } = 10;
    [Key(5)]
    public string query { get; set; } = @"";
    [Key(6)]
    public string sort_by { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
