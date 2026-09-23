namespace Obligatoris.opgave.Domain.Models;

// En klasse som arver fra Product

public class Bouquet : Product
{
    public required string FlowerType { get; set; }

    public BouquetSize BouquetSize { get; set; }

    public BookGenre BookGenre { get; set; }

    public int NumberOfBooks { get; set; }
}