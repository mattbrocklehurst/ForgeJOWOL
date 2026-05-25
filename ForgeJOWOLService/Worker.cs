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
namespace ForgeJOWOLService;

using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ForgejoOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        
        public Worker(ILogger<Worker> logger, IOptions<ForgejoOptions> options)        {
            _logger = logger;
            _httpClient = new HttpClient();
            // Configure your Forgejo details here
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("token", options.Value.AccessToken);
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
                        var json = await response.Content.ReadAsStringAsync(stoppingToken);
                        _logger.LogInformation("Successfully polled Forgejo API.");
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
    }
