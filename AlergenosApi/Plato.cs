namespace AlergenosApi
{
    // Clase creada para facilitar la información que se visualiza en el response de la API
    public class Plato
    {
        public string? Item { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public List<AlergenosJSON>? Allergens { get; set; }

    }
}
