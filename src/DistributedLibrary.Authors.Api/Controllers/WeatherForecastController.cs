using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DistributedLibrary.Authors.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private const string FileName = @"authors.json";
        public IEnumerable<dynamic> GetEntities()
        {
            return JsonConvert.DeserializeObject<IEnumerable<dynamic>>(System.IO.File.ReadAllText(FileName));
        }
        [HttpGet("/{authorId}")]
        public dynamic Get(int authorId)
        {

            foreach (var entity in GetEntities())
            {
                if (entity["id"] == authorId)
                {
                    return entity;
                }
            }

            return null;
        }
    }
}
