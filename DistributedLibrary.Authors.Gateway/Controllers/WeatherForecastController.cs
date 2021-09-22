using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace DistributedLibrary.Authors.Gateway.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        [HttpGet("/{id}")]
        public dynamic Get(long id)
        {
            Guid g = Guid.NewGuid();

            Book book = new Book();
            book.id = id;
            book.filePath = createFile(g); ;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "rabbit",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(book.filePath +"%"+ book.id.ToString());

                channel.BasicPublish(exchange: "",
                    routingKey: "rabbit",
                    basicProperties: null,
                    body: body);
            }

            return book; 
        }

        private string createFile(Guid g)
        {
            string path = $"./{g}.txt";
            FileInfo fi = new FileInfo(path);
            try
            {
                // Check if file already exists. If yes, delete it.     
                if (fi.Exists)
                {
                    fi.Delete();
                }

                // Create a new file     
                using (FileStream fs = fi.Create())
                {
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return $"{fi.FullName}";
        }
    }
}
