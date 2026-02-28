using duAn1.Models;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public int Id { get; set; }

    public bool Actived { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public string? Name { get; set; }

    public int? Price { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    public Category? Category { get; set; }

}