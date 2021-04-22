using System;

namespace Goober.Caching.Models
{
    public class CachedEntryInfo
    {
        public DateTime? NextRefreshDateTime { get; set; }

        public int? RefreshTimeInMinutes { get; set; }

        public DateTime? LastRefreshDateTime { get; set; }

        public DateTime? ExpirationDateTime { get; set; }

        public int? ExpirationTimeInMinutes { get; set; }

        public DateTime? LastAccessDateTime { get; set; }

        public DateTime RowCreatedDateTime { get; set; }

        public bool IsEmpty { get; set; }
    }
}
