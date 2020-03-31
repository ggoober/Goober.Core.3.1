using System;

namespace Goober.Core.Services.Implementation
{
    class DateTimeService : IDateTimeService
    {
        public DateTime GetDateTimeNow()
        {
            return DateTime.Now;
        }
    }
}
