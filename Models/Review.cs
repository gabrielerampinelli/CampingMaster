using System.Text.Json.Serialization;

namespace CampingMaster.Models
{
    public class Review
    {
        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; }

        [JsonPropertyName("profile_photo_url")]
        public string ProfilePhotoUrl { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("relative_time_description")]
        public string RelativeTimeDescription { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }
    }
}
