using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Stash.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PullRequestStatusNotifier.Services
{
    public class StalePullRequestNotificationService : BackgroundService
    {
        private readonly ILogger<StalePullRequestNotificationService> _logger;
        private readonly ISlackService _slackService;
        private readonly IStashService _stashService;
        private readonly TimeSpan _resendNotificationTimeout;
        private readonly TimeSpan _getStalePullRequestsTimeout;


        private readonly Dictionary<string, (PullRequest pullRequest, DateTime? lastNotificationDate)> _processedPullRequestsData;


        public StalePullRequestNotificationService(
            ILogger<StalePullRequestNotificationService> logger, 
            IServiceProvider services, 
            IConfiguration configuration)
        {
            _logger = logger;
            _slackService = services.GetRequiredService<ISlackService>();
            _stashService = services.GetRequiredService<IStashService>();
            _resendNotificationTimeout = TimeSpan.Parse(configuration["General:ResendNotificationTimeout"]);
            _getStalePullRequestsTimeout = TimeSpan.Parse(configuration["General:GetStalePullRequestsTimeout"]);

            _processedPullRequestsData =
                new Dictionary<string, (PullRequest pullRequest, DateTime? lastNotificationDate)>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    var stalePullRequests = (await _stashService.GetStalePullRequestsAsync()).ToList();

                    SendNotifications(stalePullRequests);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception was thrown!");
                }
                finally
                {
                    await Task.Delay(_getStalePullRequestsTimeout, stoppingToken);
                }
            }
        }

        private void SendNotifications(List<PullRequest> openPullRequests)
        {
            var pullRequestToNotificationIds = _processedPullRequestsData.Select(x => x.Key).ToList();
            var openPullRequestsIds = openPullRequests.Select(x => x.Id).ToList();
            var pullRequestsIdsToRemove = pullRequestToNotificationIds.Except(openPullRequestsIds).ToList();

            foreach (var pullRequestId in pullRequestsIdsToRemove)
            {
                _processedPullRequestsData.Remove(pullRequestId);
            }

            foreach (var pullRequest in openPullRequests)
            {
                var pullRequestId = pullRequest.Id;
                var now = DateTime.UtcNow;

                if (_processedPullRequestsData.ContainsKey(pullRequestId))
                {
                    var lastNotificationDate = _processedPullRequestsData[pullRequestId].lastNotificationDate;

                    if (now - lastNotificationDate > _resendNotificationTimeout)
                    {
                        lastNotificationDate = now;
                        _slackService.SendMissedPullRequestNotification(pullRequest);
                        _logger.LogInformation($"Notification was sent at: {DateTimeOffset.Now} with PR's Id {pullRequest.Id}");
                    }

                    _processedPullRequestsData[pullRequestId] = (pullRequest, lastNotificationDate);
                }
                else
                {
                    _processedPullRequestsData.Add(pullRequestId, (pullRequest, now));
                    _slackService.SendMissedPullRequestNotification(pullRequest);
                    _logger.LogInformation($"Notification was sent at: {DateTimeOffset.Now} with PR's Id {pullRequest.Id}");
                }
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogCritical($"{nameof(StalePullRequestNotificationService)} is stopping");
            
            return Task.CompletedTask;
        }
    }
}
