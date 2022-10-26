// See https://aka.ms/new-console-template for more information
using Confluent.Kafka;
using Models;
using System.Text.Json;

Console.WriteLine("Kafka consumer console app: Hello, World!");

var config = new ConsumerConfig
{
    BootstrapServers = "vs-kafka-broker:9092",
    GroupId = "kafka-dotnet-consumerGroup",
    AutoOffsetReset = AutoOffsetReset.Earliest,
    AllowAutoCreateTopics = true
};


const string topic = "purchases";

CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // prevent the process from terminating.
    cts.Cancel();
};

using (var consumer = new ConsumerBuilder<string, string>(config).Build())
{
    consumer.Subscribe(topic);
    try
    {
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Kafka consumer is starting... Ready to read messages");
        while (true)
        {
            try
            {

                var cr = consumer.Consume(cts.Token);
                Console.WriteLine($"Consumed event from topic {topic} with key {cr.Message.Key} and value {cr.Message.Value}");

                // TODO: simulate processing of data and save to database
                var message = JsonSerializer.Deserialize<KafkaMessage>(cr.Message.Value);
            }
            catch (ConsumeException ce)
            {
                Console.WriteLine("Unable to consume, error: " + ce.Message);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Ctrl-C was pressed.
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Kafka consumer is shutting down.....................");
    }
    catch (Exception e)
    {
        Console.WriteLine("Other exception occured : " + e.Message);
    }
    finally
    {
        consumer.Close();
    }
}
