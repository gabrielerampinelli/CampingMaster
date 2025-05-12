using CampingMaster.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class Camping
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long OsmId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    [StringLength(500)]
    public string Address { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string Description { get; set; }

    public double? Rating { get; set; }

    public string PhotoUrlsJson { get; set; }

    [NotMapped]
    public List<string> PhotoUrls
    {
        get => string.IsNullOrEmpty(PhotoUrlsJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(PhotoUrlsJson, JsonOptions);
        set => PhotoUrlsJson = JsonSerializer.Serialize(value, JsonOptions);
    }

    public string ReviewsJson { get; set; }

    [NotMapped]
    public List<Review> Reviews
    {
        get => string.IsNullOrEmpty(ReviewsJson) ? new List<Review>() : JsonSerializer.Deserialize<List<Review>>(ReviewsJson, JsonOptions);
        set => ReviewsJson = JsonSerializer.Serialize(value, JsonOptions);
    }

    [StringLength(100)]
    public string PlaceId { get; set; }

    [StringLength(30)]
    public string PhoneNumber { get; set; }

    [StringLength(255)]
    public string Website { get; set; }

    [StringLength(255)]
    public string Email { get; set; }

    public DateTime? CachedAt { get; set; } // Added CachedAt property

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    private static JsonSerializerOptions JsonOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}
