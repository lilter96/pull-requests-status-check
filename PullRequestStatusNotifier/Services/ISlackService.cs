using Atlassian.Stash.Entities;

namespace PullRequestStatusNotifier.Services
{
    internal interface ISlackService
    {
        public void SendMissedPullRequestNotification(PullRequest pullRequest);
    }
}