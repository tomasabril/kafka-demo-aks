using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage;
using Microsoft.Extensions.Configuration;

namespace WebApplication2.Controllers;

[Route("api/[controller]")]
[ApiController]
public class KafkaController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public KafkaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private readonly ProducerConfig config = new ProducerConfig
    {
        BootstrapServers = "vs-kafka-broker:9092"
    };

    const string topic = "purchases";


    [HttpPost]
    public async Task<IActionResult> PostAsync(string key, [FromForm] Purchase purchase)
    {
        //Getting Image
        var image = purchase.Image;
        var filePath = "";
        // Saving Image on Server
        if (image != null && image.Length > 0)
        {
            filePath = Path.Combine("/app/", image.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                //save to filesystem
                image.CopyTo(fileStream);

            }

            //upload to azure storage container
            var connectionString = _configuration.GetConnectionString("AzureBlob");
            var containerClient = new BlobContainerClient(connectionString, "imagestore");
            BlobClient blobClient = containerClient.GetBlobClient(image.FileName);
            FileStream fileStreamDisk = System.IO.File.OpenRead(filePath);
            await blobClient.UploadAsync(fileStreamDisk, true);
            filePath = blobClient.Uri.AbsoluteUri;
        
        }

        var message = new KafkaMessage()
        {
            Id = purchase.Id ?? 0,
            Name = purchase.Name ?? "no name",
            Price = purchase.Price ?? 0,
            Quantity = purchase.Quantity ?? 1,
            ImageURI = filePath,
            Created = DateTime.Now,
        };

        using (var producer = new ProducerBuilder<string, string>(config).Build())
        {
            var numProduced = 0;
            var errorMsg = "---";

            try
            {
                Console.WriteLine("lets try to produce");
                producer.Produce(topic, new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(message) },
                    (deliveryReport) =>
                    {
                        errorMsg = deliveryReport.Error.Code.ToString();
                        if (deliveryReport.Error.Code != ErrorCode.NoError)
                        {
                            Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}, code={deliveryReport.Error.Code}");
                            errorMsg = $"Failed to deliver message: reason={deliveryReport.Error.Reason}, code={deliveryReport.Error.Code}";
                        }
                        else
                        {
                            Console.WriteLine($"Produced event to topic {topic}: key = {key} value = {message}");
                            numProduced += 1;
                        }
                    });
            }
            catch (Exception e)
            {
                return Ok($"We got an exeption: {e.Message}");
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"{numProduced} message was produced to topic {topic}");

            if (numProduced == 0)
            {
                return Ok($"{numProduced} message was produced: error = {errorMsg}");
            }
            return Ok($"{numProduced} message was produced to topic {topic}");
        }
    }



}
