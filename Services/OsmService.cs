using System;
using System.Collections.Generic;
using System.Globalization; // Required for InvariantCulture
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web; // Required for HttpUtility.UrlEncode
using CampingMaster.Interfaces;
using CampingMaster.Models;
using Microsoft.Extensions.Configuration; // Add this using
using Microsoft.Extensions.Logging; // Optional: Add for better logging

namespace CampingMaster.Services
{
    public class OsmService : IOsmService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration; // Inject IConfiguration
        private readonly ILogger<OsmService> _logger; // Optional: Inject ILogger
        private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";
        private const string GooglePlacesApiUrl = "https://maps.googleapis.com/maps/api/place/textsearch/json"; // Using Text Search

        // Modified Constructor
        public OsmService(HttpClient http, IConfiguration configuration, ILogger<OsmService> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger; // Store logger instance
        }

        public async Task<IEnumerable<Camping>> SearchCampingsAsync(string location)
        {
            _logger.LogInformation("Attempting to search campings for location: {Location}", location);

            // 1. Get Coordinates from Google Places API
            (double lat, double lon)? coordinates = await GetCoordinatesFromGoogleAsync(location);

            if (!coordinates.HasValue)
            {
                _logger.LogWarning("Could not find coordinates for location: {Location}", location);
                return new List<Camping>(); // Return empty list if location not found
            }

            double latitude = coordinates.Value.lat;
            double longitude = coordinates.Value.lon;
            _logger.LogInformation("Found coordinates for {Location}: Lat={Latitude}, Lon={Longitude}", location, latitude, longitude);

            // 2. Construct Dynamic Overpass Query
            // Search within a radius (e.g., 20km = 20000 meters) around the found coordinates
            const int searchRadiusMeters = 30000;
            // Use InvariantCulture to ensure '.' is used as the decimal separator
            string overpassQuery = string.Format(CultureInfo.InvariantCulture, @"
[out:json][timeout:30];
(
  node[""tourism""=""camp_site""](around:{0},{1},{2});
  way[""tourism""=""camp_site""](around:{0},{1},{2});
  relation[""tourism""=""camp_site""](around:{0},{1},{2});
);
out center;", searchRadiusMeters, latitude, longitude);

            _logger.LogDebug("Constructed Overpass Query: {Query}", overpassQuery);

            // 3. Call Overpass API
            try
            {
                var content = new StringContent(overpassQuery);
                // Overpass API often expects 'application/x-www-form-urlencoded' or just the query string
                // Let's try sending it as form data which is common for Overpass frontends
                // var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", overpassQuery) });
                // Or stick to plain text if that worked before, but ensure encoding is right if needed.
                // Sticking to text/plain as per original code for now.
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");


                var response = await _http.PostAsync(OverpassApiUrl, content);
                response.EnsureSuccessStatusCode(); // Throws if HTTP request failed

                using var stream = await response.Content.ReadAsStreamAsync();
                var osmResponse = await JsonSerializer.DeserializeAsync<OverpassResponse>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var campings = osmResponse?.Elements?
                    .Where(e => e.Tags != null && e.Tags.ContainsKey("name")) // Ensure it has a name
                    .Select(e => new Camping
                    {
                        Id = e.Id,
                        Name = e.Tags["name"],
                        Latitude = e.Lat ?? e.Center?.Lat ?? 0,
                        Longitude = e.Lon ?? e.Center?.Lon ?? 0,
                        Address = GetAddressFromTags(e.Tags),
                        Description = e.Tags.ContainsKey("description") ? e.Tags["description"] : null
                    })
                    // Filter out entries where coordinates couldn't be determined (lat/lon are 0)
                    .Where(c => c.Latitude != 0 && c.Longitude != 0)
                    .ToList() ?? new List<Camping>();

                _logger.LogInformation("Found {Count} campings near {Location}", campings.Count, location);

                // Log each camping data to the console (optional)
                // foreach (var camping in campings)
                // {
                //     _logger.LogDebug($"Camping ID: {camping.Id}, Name: {camping.Name}, " +
                //                       $"Lat: {camping.Latitude}, Lon: {camping.Longitude}, " +
                //                       $"Address: {camping.Address ?? "N/A"}");
                // }

                return campings;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to Overpass API failed for location {Location}.", location);
                // Handle specific HTTP errors if needed
                return new List<Camping>(); // Return empty on error
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Overpass API response for location {Location}.", location);
                return new List<Camping>(); // Return empty on error
            }
            catch (Exception ex) // Catch other potential exceptions
            {
                _logger.LogError(ex, "An unexpected error occurred during Overpass search for location {Location}.", location);
                return new List<Camping>(); // Return empty on error
            }
        }

        // Helper method to get coordinates using Google Places Text Search API
        private async Task<(double lat, double lon)?> GetCoordinatesFromGoogleAsync(string location)
        {
            string apiKey = _configuration["GoogleApiKey"]; // Get API Key from configuration
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Google API Key is not configured. Please set 'GoogleApiKey' in configuration.");
                return null;
            }

            // Encode the location query parameter
            string encodedLocation = HttpUtility.UrlEncode(location);
            string requestUri = $"{GooglePlacesApiUrl}?query={encodedLocation}&key={apiKey}";

            _logger.LogDebug("Requesting coordinates from Google Places API: {Uri}", requestUri);

            try
            {
                var response = await _http.GetAsync(requestUri);
                response.EnsureSuccessStatusCode(); // Throw for HTTP errors

                using var stream = await response.Content.ReadAsStreamAsync();
                var googleResponse = await JsonSerializer.DeserializeAsync<GooglePlacesResponse>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Check response status and results
                if (googleResponse?.Status == "OK" && googleResponse.Results?.Count > 0)
                {
                    var firstResult = googleResponse.Results[0];
                    if (firstResult.Geometry?.Location != null)
                    {
                        return (firstResult.Geometry.Location.Lat, firstResult.Geometry.Location.Lng);
                    }
                }
                else
                {
                    _logger.LogWarning("Google Places API request for '{Location}' returned status {Status} or no results.", location, googleResponse?.Status ?? "N/A");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to Google Places API failed for location {Location}.", location);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize Google Places API response for location {Location}.", location);
            }
            catch (Exception ex) // Catch other potential exceptions
            {
                _logger.LogError(ex, "An unexpected error occurred during Google Places API call for location {Location}.", location);
            }

            return null; // Return null if coordinates couldn't be obtained
        }

        // Helper to construct a more complete address if addr:full is missing
        private static string GetAddressFromTags(Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("addr:full", out var fullAddress)) return fullAddress;

            var addressParts = new List<string>();

            if (tags.TryGetValue("addr:housenumber", out var houseNumber) &&
                tags.TryGetValue("addr:street", out var street))
            {
                addressParts.Add($"{street} {houseNumber}");
            }
            else if (tags.TryGetValue("addr:street", out street))
            {
                addressParts.Add(street);
            }

            if (tags.TryGetValue("addr:suburb", out var suburb)) addressParts.Add(suburb);
            if (tags.TryGetValue("addr:postcode", out var postcode)) addressParts.Add(postcode);
            if (tags.TryGetValue("addr:city", out var city)) addressParts.Add(city);
            if (tags.TryGetValue("addr:state", out var state)) addressParts.Add(state);
            if (tags.TryGetValue("addr:country", out var country)) addressParts.Add(country);

            return addressParts.Any() ? string.Join(", ", addressParts) : null;
        }



        // --- Helper Classes for Deserialization ---

        // Classes to deserialize Overpass API response (Existing)
        private class OverpassResponse
        {
            public List<Element> Elements { get; set; }
        }

        private class Element
        {
            public long Id { get; set; }
            public string Type { get; set; }
            public double? Lat { get; set; } // For nodes
            public double? Lon { get; set; } // For nodes
            public Center Center { get; set; } // For ways/relations
            public Dictionary<string, string> Tags { get; set; }
        }

        private class Center
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        // Classes to deserialize Google Places API Text Search response
        private class GooglePlacesResponse
        {
            public List<PlaceResult> Results { get; set; }
            public string Status { get; set; } // e.g., "OK", "ZERO_RESULTS", "REQUEST_DENIED"
            public string Error_Message { get; set; } // Populated on error
        }

        private class PlaceResult
        {
            public Geometry Geometry { get; set; }
            // Add other fields if needed, e.g., Formatted_Address, Name
            // public string Formatted_Address { get; set; }
            // public string Name { get; set; }
        }

        private class Geometry
        {
            public Location Location { get; set; }
        }

        private class Location
        {
            public double Lat { get; set; }
            public double Lng { get; set; } // Note: Google uses 'Lng', OSM uses 'Lon'
        }
    }
}