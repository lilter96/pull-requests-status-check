using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlassian.Stash;
using Atlassian.Stash.Entities;
using EnsureThat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PullRequestStatusNotifier.Helper;

namespace PullRequestStatusNotifier.Services
{
    public class StashService : IStashService
    {
        private readonly ILogger<StashService> _logger;
        private readonly StashClient _stashClient;

        private readonly string _projectName;
        private readonly string _repositoryName;
        private readonly TimeSpan _pullRequestStalenessTimeout;

        public StashService(IConfiguration configuration, ILogger<StashService> logger)
        {
            _logger = Ensure.Any.IsNotNull(logger, nameof(logger));
            var baseUrl = Ensure.Any.IsNotNull(configuration["Stash:BaseUrl"]);
            var accessToken = Ensure.Any.IsNotNull(configuration["Stash:AccessToken"]);
            _stashClient = new StashClient(baseUrl, accessToken, true);

            _projectName = Ensure.Any.IsNotNull(configuration["Stash:ProjectName"]);
            _repositoryName = Ensure.Any.IsNotNull(configuration["Stash:RepositoryName"]);
            _pullRequestStalenessTimeout = Ensure.Any.IsNotDefault(TimeSpan.Parse(configuration["General:PullRequestStalenessTimeout"]));
        }

        public async Task<IEnumerable<PullRequest>> GetStalePullRequestsAsync()
        {
            var openPullRequests = (await _stashClient.PullRequests.Get(_projectName, _repositoryName, state: PullRequestState.OPEN))
                .Values;
            _logger.LogInformation("Open pull requests was received.");
            var stalePullRequests = new List<PullRequest>();

            foreach (var pullRequest in openPullRequests)
            {
                var now = DateTime.UtcNow;
                var unixTimeSeconds = long.Parse(pullRequest.UpdatedDate);
                var currentUpdatedDate = DateTimeHelper.FromUnixTimestamp(unixTimeSeconds);

                if (now - currentUpdatedDate < _pullRequestStalenessTimeout)
                {
                    continue;
                }

                stalePullRequests.Add(pullRequest);
            }

            _logger.LogInformation("Stale pull requests was received.");
            return stalePullRequests;
        }
    }
}