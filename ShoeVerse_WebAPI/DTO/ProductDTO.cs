public class ProductDTO

{

    public int? Product_ID { get; set; }

    public int Category_ID { get; set; }

    public int Brand_ID { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }

    public string Gender { get; set; }

    public string Material { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ProductColorDTO> Colors { get; set; }

}