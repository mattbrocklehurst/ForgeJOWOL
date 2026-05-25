/*
 *                  ForgeJO CI C# Watcher Service
 *
 * Author - Matt Brocklehurst / 2026
 *          Copyright (C) 2026 Matt Brocklehurst
 *
 *  Simple service designed to sit and run monitoring if any CI jobs are queued on
 *  ForgeJO, the CI runner machine spends most of its life sleeping, if a CI job is
 *  waiting to be run, this service first checks to see if its alive (ping), if not
 *  we send the magic WOL packet to it.
 * 
 */

using System.Net.Http.Json;

namespace ForgeJOWOLService;

using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;

public class ForgejoOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string RunnerIP { get; set; } = string.Empty;
}

public record ForgejoJob(
    [property: JsonPropertyName("status")] string Status
);

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<ForgejoJob>))]
internal partial class ForgejoJsonContext : JsonSerializerContext
{
}


public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private readonly ForgejoOptions _options; 
        
        public Worker(ILogger<Worker> logger, IOptions<ForgejoOptions> options)        {
            _logger = logger;
            _options = options.Value; 
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("token", _options.AccessToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ForgeJO CI Watcher Service starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Query the actions runner jobs endpoint
                    var response = await _httpClient.GetAsync("api/v1/actions/runners/jobs", stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully polled Forgejo API.");
                        
                        var jobs = await response.Content.ReadFromJsonAsync(
                            ForgejoJsonContext.Default.ListForgejoJob, 
                            stoppingToken);

                        if (jobs?.Any(j => j.Status == "queued") == true)
                        {
                            _logger.LogInformation("Queued jobs detected!");
                
                            bool isAlive = await IsTargetAlive(_options.RunnerIP);
            
                            if (!isAlive)
                            {
                                _logger.LogInformation("Job queued and NUC is asleep. Sending WOL...");
                                SendMagicPacket(_options.MacAddress);
                            }
                            else
                            {
                                _logger.LogInformation("Job queued, but NUC is already awake.");
                            }
                        }
                        else
                        {
                            _logger.LogInformation("No jobs queued");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Failed polling Forgejo API, statusCode = " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error polling Forgejo API");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task<bool> IsTargetAlive(string ipAddress)
        {
            try
            {
                using Ping pingSender = new Ping();
            
                PingReply reply = await pingSender.SendPingAsync(ipAddress, 2000);
        
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ping failed for {IP}: {Message}", ipAddress, ex.Message);
                return false;
            }
        }
            
        public static void SendMagicPacket(string macAddress)
        {
            string cleanMac = macAddress.Replace(":", "").Replace("-", "");
            byte[] macBytes = Enumerable.Range(0, cleanMac.Length / 2)
                .Select(x => Convert.ToByte(cleanMac.Substring(x * 2, 2), 16))
                .ToArray();

            //  Build the Magic Packet
            // 6 bytes of 0xFF followed by 16 repetitions of the MAC address
            byte[] magicPacket = Enumerable.Repeat((byte)0xFF, 6)
                .Concat(Enumerable.Repeat(macBytes, 16).SelectMany(m => m))
                .ToArray();

            using (UdpClient client = new UdpClient())
            {
                client.EnableBroadcast = true;
            
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, 9);
            
                client.Send(magicPacket, magicPacket.Length, endPoint);
            }
        }
    }
