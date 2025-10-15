using System.ComponentModel.DataAnnotations;

namespace IbasSupport.Web.Models;

public class User
{
    [Range(1, int.MaxValue)]
    public int userId { get; set; }
    
    [Required, StringLength(200)]
    public string name { get; set; }
    
    [Required, EmailAddress, StringLength(200)]
    public string email { get; set; }
    
    [Phone, StringLength(10)]
    public string? phone { get; set; }
}