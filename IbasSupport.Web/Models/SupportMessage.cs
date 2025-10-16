using System.ComponentModel.DataAnnotations;

namespace IbasSupport.Web.Models;

public class SupportMessage
{
        [Required, StringLength(40)]
        public string id { get; set; }
        
        [Required, StringLength(100)]
        public string category { get; set; }
        public int? requestId { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime dateTime { get; set; }
        
        [Required, StringLength(2000)]
        public string description { get; set; }
        
        [Required]                     
        [ValidateComplexType]   
        public User user { get; set; }
        
        public Dealer? dealer { get; set; }
}