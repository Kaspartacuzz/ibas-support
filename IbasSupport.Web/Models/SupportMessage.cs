namespace IbasSupport.Web.Models;

public class SupportMessage
{
        public string id { get; set; }
        public string category { get; set; }
        public int requestId { get; set; }
        public DateTime dateTime { get; set; }
        public string description { get; set; }
        public User user { get; set; }
        public Dealer? dealer { get; set; }
}