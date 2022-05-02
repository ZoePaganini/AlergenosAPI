using Azure;
using Azure.Data.Tables;

namespace AlergenosApi
{
    // Clase para la entidad de Plato
    public class PlatoEntity : ITableEntity
    {
  
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Hotel { get; set; }
        public string Item { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public bool Active { get; set; }
        public string? Allergens { get; set; }
        public ETag ETag { get; set; }
        DateTimeOffset? ITableEntity.Timestamp { get; set; }

    }

    // Clase para recoger la información de Alérgenos proporcionada por los platos
    public class Alergenos
    {
        public string? Alergeno { get; set; }
    }

}
