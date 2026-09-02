namespace Obligatoris.opgave.Domain.Models;

//Denne klasse bruges til produktet altså vores buketer. Den indholder alle de properties. 

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string BookGenre { get; set; }
    public decimal Price { get; set; }
    public string Imagepath { get; set; }
    public string ImageDescription { get; set; }
    public bool IsPopular { get; set; }
}