using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace wam.Services
{
    public class UserActivityEvent
    {
        public DateTime TimeCreated { get; set; }
        public string EventType { get; set; } // Login / Logout
        public string UserName { get; set; }
    }

    public class UserActivityService
    {
        public static List<UserActivityEvent> GetLoginLogoutEvents(int maxCount = 50)
        {
            List<UserActivityEvent> events = new List<UserActivityEvent>();

            // Security logları
            string queryString = "*[System[(EventID=4624 or EventID=4634)]]";
            EventLogQuery eventsQuery = new EventLogQuery("Security", PathType.LogName, queryString);

            try
            {
                using (EventLogReader logReader = new EventLogReader(eventsQuery))
                {
                    EventRecord eventInstance;
                    int count = 0;

                    while ((eventInstance = logReader.ReadEvent()) != null && count < maxCount)
                    {
                        string user = eventInstance.Properties.Count > 5 ? eventInstance.Properties[5].Value.ToString() : "Bilinmiyor";
                        string type = eventInstance.Id == 4624 ? "Login" : "Logout";

                        events.Add(new UserActivityEvent
                        {
                            TimeCreated = eventInstance.TimeCreated ?? DateTime.MinValue,
                            EventType = type,
                            UserName = user
                        });

                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
            }

            return events;
        }
    }
}
