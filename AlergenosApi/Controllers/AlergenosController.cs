using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlergenosApi.Controllers
{
    // Ruta base de la API
    [Route("api/[controller]")]
    [ApiController]
    public class AlergenosController : ControllerBase
    {
        // Connection String
        private readonly string connectionString = "DefaultEndpointsProtocol=https;AccountName=dvldwvalidation;AccountKey=8CUxExiwJdLfvgTal8RQv1wQHmeazj1f36AzFtH3VsyiAV0JvSy+tdGPAix1lbnRj8ztiT+GSU+F3h1ryWCQRw==;BlobEndpoint=https://dvldwvalidation.blob.core.windows.net/;QueueEndpoint=https://dvldwvalidation.queue.core.windows.net/;TableEndpoint=https://dvldwvalidation.table.core.windows.net/;FileEndpoint=https://dvldwvalidation.file.core.windows.net/;";

        // Método para conseguir la información de todos los platos de un hotel, junto con los alérgenos
        // GET api/Alergenos/{hotel}
        [HttpGet("{hotel}")]
        public IActionResult GetPlatosHotel(string hotel)
        {
            TableClient clientPlatos = new TableClient(connectionString, "ItemAllergens");
            TableClient clientAlergenos = new TableClient(connectionString, "Allergens");
            Pageable<PlatoEntity> platosAPI = clientPlatos.Query<PlatoEntity>(filter: $"PartitionKey eq '{hotel}'");
            Pageable<AlergenoEntity> alergenosAPI = clientAlergenos.Query<AlergenoEntity>();
            List<Plato> platos = new();
            List<AlergenoEntity> alergenosES = new();
            List<AlergenoEntity> alergenosEN = new();
            List<AlergenoEntity> alergenosDE = new();
            
            foreach (AlergenoEntity alergeno in alergenosAPI)
            {
                switch(alergeno.PartitionKey)
                {
                    case "ES": 
                        alergenosES.Add(alergeno);
                        break;
                    case "DE": 
                        alergenosDE.Add(alergeno);
                        break;
                    case "EN":
                        alergenosEN.Add(alergeno);
                        break;
                }
            }

            foreach (PlatoEntity plato in platosAPI)
            {
                List<AlergenosJSON> alergenosJSON = new();
                if (plato.Allergens != null && plato.Allergens.Length > 0) {
                    List<Alergenos> platoAlergenos = JsonSerializer.Deserialize<List<Alergenos>>(plato.Allergens)!;
                    foreach (Alergenos alergeno in platoAlergenos)
                    {
                        var esp = alergenosES.Find(x => x.RowKey == alergeno.Alergeno);
                        var de = alergenosDE.Find(x => x.RowKey == alergeno.Alergeno);
                        var en = alergenosEN.Find(x => x.RowKey == alergeno.Alergeno);
                        alergenosJSON.Add(
                            new AlergenosJSON()
                            {
                                AlergenoDe = de?.Description,
                                AlergenoEs = esp?.Description,
                                AlergenoEn = en?.Description,
                            }
                            );
                    }
                }
                
                platos.Add(new Plato()
                {
                    Allergens = alergenosJSON,
                    Item = plato.Item,
                    Description = plato.Description,
                    Type = plato.Type
                });
            }
            return Ok(platos);
        }

        // Método para recoger la información de un plato
        // GET api/Alergenos/{hotel}/plato/{nombre_plato}
        [HttpGet("{hotel}/plato/{nombre_plato}")]
        public IActionResult GetPlatos(string hotel, string nombre_plato)
        {
            TableClient client = new TableClient(connectionString, "Alergenos");
            Pageable<PlatoEntity> entities = client.Query<PlatoEntity>(filter: $"Hotel eq {hotel}");
            List<Plato> platos = new();
            
            foreach (PlatoEntity plato in entities)
            {
                if (plato.Description.ToLower().Contains(nombre_plato.ToLower()))
                {
                    platos.Add(new Plato()
                    {
                        //Allergens = (plato.Allergens != null) ? JsonSerializer.Deserialize<List<Alergenos>>(plato.Allergens) : null,
                        Item = plato.Item,
                        Description = plato.Description,
                        Type = plato.Type
                    });
                }
            }
            return Ok(platos);
        }

        // Método para recoger todos los platos que contengan un tipo de alérgeno
        // GET api/Alergenos/{hotel}/alergeno/{alergeno}
        [HttpGet("{hotel}/alergeno/{alergeno}")]
        public IActionResult GetAlergenos(string hotel, string alergeno)
        {
            TableClient client = new TableClient(connectionString, "Alergenos");
            Pageable<PlatoEntity> entities = client.Query<PlatoEntity>(filter: $"Hotel eq '{hotel}'");
            List<Plato> platos = new();
            
            foreach (PlatoEntity plato in entities)
            {
                List<Alergenos> alergenosList = (plato.Allergens != null) ? JsonSerializer.Deserialize<List<Alergenos>>(plato.Allergens) : null;
                if (alergenosList != null)
                {
                    foreach (Alergenos a in alergenosList)
                    {
                        if (a.Alergeno.Contains(alergeno.ToUpper()))
                        {
                            platos.Add(new Plato()
                            {
                                //Allergens = alergenosList,
                                Item = plato.Item,
                                Description = plato.Description,
                                Type = plato.Type
                            });
                        }
                    }
                }
            }
            return Ok(platos);
        }
    }
}
