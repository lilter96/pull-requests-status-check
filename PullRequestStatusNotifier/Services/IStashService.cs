using System.Collections.Generic;
using System.Threading.Tasks;
using Atlassian.Stash.Entities;

namespace PullRequestStatusNotifier.Services
{
    internal interface IStashService
    { 
        public Task<IEnumerable<PullRequest>> GetStalePullRequestsAsync();
    }
}