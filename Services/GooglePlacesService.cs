// --------------------------------------------------
// GooglePlacesService.cs
// --------------------------------------------------
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class GooglePlacesService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GooglePlacesService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["GooglePlaces:ApiKey"];
    }

    public async Task<string> GetCampingPhotoUrlAsync(double latitude, double longitude)
    {
        // 1) Nearby Search for campgrounds near the exact coords
        var nearbyUrl = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json" +
                        $"?location={latitude},{longitude}" +
                        $"&radius=500" +
                        $"&type=campground" +
                        $"&key={_apiKey}";

        var nearbyJson = JObject.Parse(await _httpClient.GetStringAsync(nearbyUrl));
        var firstResult = nearbyJson["results"]?.First;
        if (firstResult == null)
            return GetDefaultPhotoUrl();

        // 2) Extract the first photo_reference (if any)
        var photoRef = firstResult["photos"]?.First?["photo_reference"]?.Value<string>();
        if (string.IsNullOrEmpty(photoRef))
            return GetDefaultPhotoUrl();

        // 3) Build and return the cached URL
        return $"https://maps.googleapis.com/maps/api/place/photo" +
               $"?maxwidth=400" +
               $"&photoreference={photoRef}" +
               $"&key={_apiKey}";
    }

    private string GetDefaultPhotoUrl()
    {
        // fallback image in case no photo is found
        return "/images/no-camping-photo.png";
    }
}
