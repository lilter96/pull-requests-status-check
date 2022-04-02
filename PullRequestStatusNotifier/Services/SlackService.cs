using System;
using System.Collections.Generic;
using System.Linq;
using Atlassian.Stash.Entities;
using EnsureThat;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PullRequestStatusNotifier.Helper;
using Slack.Webhooks;

namespace PullRequestStatusNotifier.Services
{
    public class SlackService : ISlackService
    {
        private readonly ILogger<SlackService> _logger;
        private readonly SlackClient _slackApiClient;
        private readonly string _repositoryName;
        private readonly string _channelName;
        private readonly string _groupId;

        public SlackService(IConfiguration configuration, ILogger<SlackService> logger)
        {
            Ensure.Any.IsNotNull(configuration);
            _logger = Ensure.Any.IsNotNull(logger, nameof(logger));
            _slackApiClient = new SlackClient(configuration["Slack:WebHookUrl"]);

            _repositoryName = Ensure.Any.IsNotNull(configuration["Stash:RepositoryName"]);
            _channelName = Ensure.Any.IsNotNull(configuration["Slack:ChannelName"]);
            _groupId = Ensure.Any.IsNotNull(configuration["Slack:MentioningGroupId"]);
        }

        public void SendMissedPullRequestNotification(PullRequest pullRequest)
        {
            Ensure.Any.IsNotNull(pullRequest);
            _logger.LogInformation("Create message for the slack channel.");
            _slackApiClient.Post(CreateNotificationAboutPullRequest(pullRequest));
            _logger.LogInformation("Message was sent to the slack channel.");
        }

        private SlackMessage CreateNotificationAboutPullRequest(PullRequest pullRequest)
        {
            var notUpdatedTime = DateTime.Now
                .Subtract(DateTimeHelper.FromUnixTimestamp(long.Parse(pullRequest.UpdatedDate))).Ticks;

            var timeSpan = TimeSpan.FromTicks(notUpdatedTime);
            
            return new SlackMessage
            {
                Channel = _channelName,
                IconEmoji = Emoji.Warning,
                Username = _repositoryName,
                Markdown = true,
                Text = $"<!subteam^{_groupId}>",
                Attachments = new List<SlackAttachment>
                {
                    new()
                    {
                        Title = "Attention",
                        Fallback = $"The pull request is not updated within *{timeSpan.Humanize(3)}*:\n<{pullRequest.Links.Self.First().Href}|{pullRequest.Title}>",
                        Text = $"The pull request is not updated within *{timeSpan.Humanize(3)}*:\n<{pullRequest.Links.Self.First().Href}|{pullRequest.Title}>",
                        Color = "#D00000",
                        Fields = new List<SlackField>
                        {
                            new()
                            {
                                Title = "*Please take care of this pull request!*"
                            }
                        }
                    }
                }
            };
        }
    }
}