using System;

namespace CustomerRegistration
{
    public class Interview
    {
        public string Name { get; internal set; }
        public DateTime ScheduledTime { get; internal set; }
        public Guid Id { get; internal set; }
    }
}