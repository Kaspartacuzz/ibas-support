using System.ComponentModel.DataAnnotations;

namespace IbasSupport.Web.Models;

public class Dealer
{ 
    [Range(0, int.MaxValue)]
    public int dealerId { get; set; } 
    
    [Required, StringLength(200)]
    public string name { get; set; } 
    
    [StringLength(300)]
    public string? address { get; set; } 
    
    [Phone, StringLength(10)]
    public string? phone { get; set; }
}