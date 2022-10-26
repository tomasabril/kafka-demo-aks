using Microsoft.AspNetCore.Http;

namespace Models;

public class Purchase
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public IFormFile? Image { get; set; }
}
