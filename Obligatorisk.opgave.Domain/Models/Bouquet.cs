namespace Obligatoris.opgave.Domain.Models;

//En klasse som arver fra product 

public class Bouquet : Product
{
    public string FlowerType { get; set; }
    public string BouquetSize { get; set; }
    public string BookGenre { get; set; }
    
}