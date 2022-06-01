using Azure;
using Azure.Data.Tables;
using System;


namespace AlergenosApi
{
    public class TPVEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Hotel { get; set; }
        public string TPV { get; set; }
        public string Descripcion { get; set; }
        public ETag ETag { get; set; }
        DateTimeOffset? ITableEntity.Timestamp { get; set; }

    }
}
