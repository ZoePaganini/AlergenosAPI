using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
        private IConfiguration configuration;
        private string connectionString;

        public AlergenosController(IConfiguration iConfig)
        {
            configuration = iConfig;
            SecretClientOptions options = new() { Retry = { MaxRetries = 6, Delay = TimeSpan.FromSeconds(2), MaxDelay = TimeSpan.FromSeconds(16), Mode = Azure.Core.RetryMode.Exponential } };
            SecretClient secretClient = new(new Uri(configuration.GetValue<string>("KeyVaultUrl")), new DefaultAzureCredential(), options);
            connectionString = secretClient.GetSecret("BDConnectionString").Value.Value;
        }

        // Método para conseguir la información de todos los platos de un hotel, junto con los alérgenos
        // GET api/Alergenos/{hotel}
        [HttpGet("{hotel}")]
        public async Task<IActionResult> GetPlatosHotel(string hotel)
        {
            DateTime mesesAtras = DateTime.Now.AddMonths(-6);
            string mesesTimeStamp = mesesAtras.ToString("yyyy-MM-ddTHH:mmZ");
            TableClient clientPlatos = new TableClient(connectionString, "ItemAllergens");
            TableClient clientAlergenos = new TableClient(connectionString, "Allergens");
            TableClient clientTPVs = new TableClient(connectionString, "POS");
            Pageable<PlatoEntity> platosAPI = clientPlatos.Query<PlatoEntity>(filter: $"(Hotel eq '{hotel}' and Active eq true) or (Hotel eq '{hotel}' and Active eq false and Timestamp ge datetime'{mesesTimeStamp}')");
            Pageable<AlergenoEntity> alergenosAPI = clientAlergenos.Query<AlergenoEntity>();
            Pageable<TPVEntity> tpvsAPI = clientTPVs.Query<TPVEntity>(filter: $"Hotel eq '{hotel}'");
            List<TPV> tpvs = new();
            List<Plato> platos = new();
            List<AlergenoEntity> alergenosES = new();
            List<AlergenoEntity> alergenosEN = new();
            List<AlergenoEntity> alergenosDE = new();

            foreach (TPVEntity p in tpvsAPI)
            {
                tpvs.Add(new TPV()
                {
                    Codigo = p.TPV,
                    Descripcion = p.Descripcion,
                });
            }

            foreach (AlergenoEntity alergeno in alergenosAPI)
            {
                switch (alergeno.PartitionKey)
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
                if (plato.Allergens != null && plato.Allergens.Length > 0)
                {
                    List<string> platoAlergenos = JsonSerializer.Deserialize<List<string>>(plato.Allergens)!;
                    foreach (var alergeno in platoAlergenos)
                    {
                        var esp = alergenosES.Find(x => x.RowKey == alergeno);
                        var de = alergenosDE.Find(x => x.RowKey == alergeno);
                        var en = alergenosEN.Find(x => x.RowKey == alergeno);
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

                var tpvDescription = tpvs.Find(tpv => tpv.Codigo!.Equals(plato.TPV));


                platos.Add(new Plato()
                {
                    Allergens = alergenosJSON,
                    Item = plato.Item,
                    Description = plato.Description.Trim(),
                    Type = plato.Type,
                    TPV = tpvDescription,
                });
            }
            var platosOrdenados = platos.OrderBy(plato => plato.Description);
            return Ok(platosOrdenados);
        }

        //GET api/Alergenos/{hotel}/tpvs
        [HttpGet("{hotel}/tpvs")]
        public IActionResult GetTPVs(string hotel)
        {
            TableClient clientTPVs = new TableClient(connectionString, "POS");
            Pageable<TPVEntity> tpvsAPI = clientTPVs.Query<TPVEntity>(filter: $"Hotel eq '{hotel}'");
            List<TPV> tpvs = new();
            foreach (TPVEntity p in tpvsAPI)
            {
                tpvs.Add(new TPV()
                {
                    Codigo = p.TPV,
                    Descripcion = p.Descripcion,
                });
            }
            return Ok(tpvs);
        }

        // Método para recoger la información de un plato
        // GET api/Alergenos/{hotel}/plato/{nombre_plato}
        [HttpGet("{hotel}/plato/{nombre_plato}")]
        public IActionResult GetPlatos(string hotel, string nombre_plato)
        {
            TableClient client = new TableClient(connectionString, "Alergenos");
            Pageable<PlatoEntity> entities = client.Query<PlatoEntity>(filter: $"PartitionKey eq {hotel}");
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
