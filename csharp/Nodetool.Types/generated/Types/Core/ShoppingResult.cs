using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class ShoppingResult
{
    [Key(0)]
    public object badge { get; set; } = null;
    [Key(1)]
    public object delivery { get; set; } = null;
    [Key(2)]
    public object extensions { get; set; } = null;
    [Key(3)]
    public object extracted_old_price { get; set; } = null;
    [Key(4)]
    public object extracted_price { get; set; } = null;
    [Key(5)]
    public object link { get; set; } = null;
    [Key(6)]
    public object old_price { get; set; } = null;
    [Key(7)]
    public int position { get; set; }
    [Key(8)]
    public object price { get; set; } = null;
    [Key(9)]
    public object product_id { get; set; } = null;
    [Key(10)]
    public object product_link { get; set; } = null;
    [Key(11)]
    public object rating { get; set; } = null;
    [Key(12)]
    public object reviews { get; set; } = null;
    [Key(13)]
    public object source { get; set; } = null;
    [Key(14)]
    public object source_icon { get; set; } = null;
    [Key(15)]
    public object store_rating { get; set; } = null;
    [Key(16)]
    public object store_reviews { get; set; } = null;
    [Key(17)]
    public object tag { get; set; } = null;
    [Key(18)]
    public object thumbnail { get; set; } = null;
    [Key(19)]
    public object title { get; set; } = null;
    [Key(20)]
    public object type { get; set; } = @"shopping_result";
}
