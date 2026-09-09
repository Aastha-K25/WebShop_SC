namespace Obligatoris.opgave.Domain.Models;

//Denne klasse er en abstrakt for alle produkter i webshoppen 
//De har en fælles produktegenskaber. 

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string ImagePath { get; set; }
    public string ImageDescription { get; set; }
    public bool IsPopular { get; set; }
    public int StockQuantity { get; set; }
    
}