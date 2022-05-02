using Azure;
using Azure.Data.Tables;

namespace AlergenosApi
{
    public class AlergenoEntity : ITableEntity
    {

        public string PartitionKey { get; set; } = "ES";
        public string RowKey { get; set; } = "0SINALERGENOS";
        public string? Description { get; set; }
        public ETag ETag { get; set; }
        DateTimeOffset? ITableEntity.Timestamp { get; set; }

    }

    public class AlergenosJSON
    {
        public string? AlergenoEs { get; set; }
        public string? AlergenoEn { get; set; }
        public string? AlergenoDe { get; set; }
    }
}
