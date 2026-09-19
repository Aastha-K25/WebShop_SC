namespace Obligatoris.opgave.Domain.Models;

//En klasse som arver fra product 

public class Bouquet : Product
{
    public required string FlowerType { get; set; }
    public required string BouquetSize { get; set; }
    public required string BookGenre { get; set; }
    public int NumberOfBooks { get; set; }
    
}