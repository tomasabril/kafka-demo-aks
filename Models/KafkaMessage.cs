namespace Models;

public class KafkaMessage
{
    public long Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string ImageURI { get; set; }
    public DateTime Created { get; set; }

}
