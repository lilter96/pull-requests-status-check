using System;

namespace PullRequestStatusNotifier.Helper
{
    public static class DateTimeHelper
    {
        public static DateTime FromUnixTimestamp(long unixTimeStamp)
        {
            var dtDateTime = DateTime.UnixEpoch;
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }
    }
}