using System.ComponentModel;
using LinkyTIC.API;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics.Metrics;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Text.Json;

namespace LinkyTIC.WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private IMqttClient? mqttClient;
    private readonly List<Frame> frames = new List<Frame>();

    public Worker(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Initialize Mqtt Client");
        // Create TCP based options using the builder.
        var options = new MqttClientOptionsBuilder()
            .WithClientId("home-server")
            .WithTcpServer("frobin.ovh")
            .WithCredentials("frobin", "kVC-cghsn76CD-dcc")
            .WithCleanSession()
            .Build();

        InitializeMqttClient();
        logger.LogInformation("Mqtt Client Initialized");

        logger.LogInformation("Creating reader");
        LinkyTIC.API.Reader reader = new API.Reader(logger);
        reader.FrameReceived += Reader_FrameReceived;

        logger.LogInformation("Starting reader");

        reader.Start("/dev/ttyAMA0", API.TICMode.Standard);

        logger.LogInformation("Reader started");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            var sendingFrames = frames.ToList();
            frames.Clear();

            try
            {
                // This code will also do the very first connect! So no call to _ConnectAsync_ is required in the first place.
                if (!await mqttClient.TryPingAsync())
                {
                    await mqttClient!.ConnectAsync(options, stoppingToken);

                    // Subscribe to topics when session is clean etc.
                    logger.LogInformation("The MQTT client is connected.");
                }

                try
                {
                    var message = new MqttApplicationMessageBuilder()
                    .WithTopic("power")
                    .WithPayload(JsonSerializer.Serialize(sendingFrames.Select(_ => ConvertFrameToMessage(_)).ToList(), new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                    .WithRetainFlag()
                    .Build();

                    await mqttClient!.PublishAsync(message, CancellationToken.None);

                    logger.LogInformation("Sent {count} frames", sendingFrames.Count);

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish the frame stack ({count} frames)", sendingFrames.Count);
                    frames.AddRange(sendingFrames);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception properly (logging etc.).
                logger.LogError(ex, "The MQTT client  connection failed");
            }
        }

        reader.Stop();
    }

    private void InitializeMqttClient()
    {
        var factory = new MqttFactory();
        mqttClient = factory.CreateMqttClient();

        //// Create TCP based options using the builder.
        //var options = new MqttClientOptionsBuilder()
        //    .WithClientId("home-server")
        //    .WithTcpServer("frobin.ovh")
        //    .WithCredentials("frobin", "kVC-cghsn76CD-dcc")
        //    .WithCleanSession()
        //    .Build();

        //mqttClient.DisconnectedAsync += (async e =>
        //{
        //    logger.LogInformation("Disconnected from server");
        //    await Task.Delay(TimeSpan.FromSeconds(5));

        //    try
        //    {
        //        await mqttClient.ConnectAsync(options, stoppingToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogInformation(ex, "Reconnecting failed");
        //    }
        //});

        //await mqttClient.ConnectAsync(options, stoppingToken);
    }

    private object ConvertFrameToMessage(Frame frame)
    {
        var tag_prm = frame[GroupLabels.PRM]?.Value;
        var totalCounter = frame[GroupLabels.EAST]?.IntValue;
        var rmsCurrent = frame[GroupLabels.IRMS1]?.IntValue;
        var rmsVoltage = frame[GroupLabels.URMS1]?.IntValue;
        var instantaneousPowerDrawn = frame[GroupLabels.SINSTS]?.IntValue;

        return new
        {
            date = new DateTimeOffset(frame[GroupLabels.DATE]!.DateTime!.Value).ToUnixTimeSeconds(),
            measurement = "power_consumption",
            tag_prm,
            totalCounter,
            rmsCurrent,
            rmsVoltage,
            instantaneousPowerDrawn
        };
    }

    private void Reader_FrameReceived(object? sender, API.Reader.FrameReceivedEventArgs e)
    {
        if (e.Frame[GroupLabels.DATE] is not null)
        {
            frames.Add(e.Frame);
        }
    }
}

