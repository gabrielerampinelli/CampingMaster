// Pages/Index.cshtml.cs
using CampingMaster.Data;
using CampingMaster.Interfaces; // You might not need this if IPasswordHasherService was the only thing here
using CampingMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class IndexModel : PageModel
{
    private readonly IOsmService _campflare;
    private readonly ApplicationDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IAntiforgery _antiforgery;
    // private readonly IPasswordHasherService _passwordHasher; // REMOVED

    // IMPORTANT: Move API Keys to configuration
    private const string GoogleApiKey = "AIzaSyBG5yNMgZNaN8hzAj6cWvTHM8DbeBmq-fA"; // Replace!
    private const string GeminiApiKey = "AIzaSyBG5yNMgZNaN8hzAj6cWvTHM8DbeBmq-fA";   // Replace!

    public List<Camping> Campings { get; set; } = new();
    public string Query { get; set; }
    public User CurrentUser { get; set; }
    public List<string> Suggestions = new List<string>();
    public bool IsAdmin { get; set; }
    public List<User> AllUsers { get; set; } = new();

    public IndexModel(
        IOsmService campflare,
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IAntiforgery antiforgery)
    // IPasswordHasherService passwordHasher) // REMOVED from constructor
    {
        _campflare = campflare;
        _db = db;
        _httpClient = httpClientFactory.CreateClient();
        _antiforgery = antiforgery;
        // _passwordHasher = passwordHasher; // REMOVED
    }

    public async Task OnGetAsync(string query)
    {
        var username = User.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            CurrentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (CurrentUser != null && CurrentUser.Role == "Admin")
            {
                IsAdmin = true;
                ViewData["Title"] = "Admin - User Management";
                AllUsers = await _db.Users.OrderBy(u => u.Username).ToListAsync();
                return;
            }
        }
        IsAdmin = false;
        ViewData["Title"] = "CampingMaster - Find Your Perfect Camping Spot";
        Query = query;
        if (string.IsNullOrWhiteSpace(Query)) return;

        var campingsData = await _campflare.SearchCampingsAsync(Query);
        foreach (var campingEntry in campingsData)
        {
            if (string.IsNullOrWhiteSpace(campingEntry.Address) || campingEntry.Address == "No Address") continue;
            if (string.IsNullOrWhiteSpace(campingEntry.Description)) campingEntry.Description = "No Description Available";
            var cached = await _db.Campings.AsNoTracking().FirstOrDefaultAsync(c => c.Name == campingEntry.Name);
            if (cached != null) { Campings.Add(cached); continue; }
            var placeId = await GetPlaceIdAsync(campingEntry.Name, campingEntry.Latitude, campingEntry.Longitude);
            var details = !string.IsNullOrEmpty(placeId) ? await GetPlaceDetailsAsync(placeId) : (null, new List<string>(), new List<Review>(), null, null);
            campingEntry.Rating = details.Item1; 
            campingEntry.PhotoUrlsJson = JsonSerializer.Serialize(details.Item2); 
            campingEntry.ReviewsJson = JsonSerializer.Serialize(details.Item3);
            campingEntry.PhoneNumber = details.Item4 ?? "Not available"; // Add default value for phone number
            campingEntry.Website = details.Item5 ?? ""; // Empty string if NULL
            campingEntry.CachedAt = DateTime.UtcNow; 
            campingEntry.PlaceId = placeId ?? "No Place Id";
            campingEntry.Id = 0; // Reset ID so database can auto-generate it
            campingEntry.Email = "no-email@example.com"; // Add default email value

            _db.Campings.Add(campingEntry);
            await _db.SaveChangesAsync();
            Campings.Add(campingEntry);
        }
    }

    private async Task<string> GetPlaceIdAsync(string name, double lat, double lng)
    {
        var uri = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={Uri.EscapeDataString(name)}&location={lat},{lng}&radius=5000&key={GoogleApiKey}";
        var resp = await _httpClient.GetAsync(uri); if (!resp.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()); var results = doc.RootElement.GetProperty("results");
        return results.GetArrayLength() > 0 && results[0].TryGetProperty("place_id", out var placeId) ? placeId.GetString() : null;
    }

    private async Task<(double?, List<string>, List<Review>, string, string)> GetPlaceDetailsAsync(string placeId)
    {
        var uri = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=rating,reviews,photos,formatted_phone_number,website&key={GoogleApiKey}";
        var resp = await _httpClient.GetAsync(uri); if (!resp.IsSuccessStatusCode) return (null, new List<string>(), new List<Review>(), null, null);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()); var result = doc.RootElement.GetProperty("result");
        double? rating = result.TryGetProperty("rating", out var r) ? r.GetDouble() : null; var photoUrls = new List<string>();
        if (result.TryGetProperty("photos", out var photos)) foreach (var p in photos.EnumerateArray()) photoUrls.Add($"https://maps.googleapis.com/maps/api/place/photo?maxwidth=400&photoreference={p.GetProperty("photo_reference").GetString()}&key={GoogleApiKey}");
        var reviews = new List<Review>();
        if (result.TryGetProperty("reviews", out var revs)) foreach (var rv in revs.EnumerateArray()) reviews.Add(new Review { AuthorName = rv.GetProperty("author_name").GetString(), ProfilePhotoUrl = rv.TryGetProperty("profile_photo_url", out var pu) ? pu.GetString() : null, Text = rv.GetProperty("text").GetString(), Rating = rv.GetProperty("rating").GetInt32() });
        string phone = result.TryGetProperty("formatted_phone_number", out var ph) ? ph.GetString() : null; string web = result.TryGetProperty("website", out var w) ? w.GetString() : null;
        return (rating, photoUrls, reviews, phone, web);
    }

    private async Task<List<string>> GetAISuggestionsAsync()
    {
        var uri = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={GeminiApiKey}";
        var requestBody = new { contents = new[] { new { parts = new[] { new { text = "Dammi i nomi di 5 località specifiche fantastiche per fare campeggio in Europa! Volgio ogni volta nomi diversi, è soprattutto, voglio solo i nomi in italiano così posso estrarli da codice senza intrusioni di altre parole!" } } } }, generationConfig = new { temperature = 2.0 } };
        var requestJson = JsonSerializer.Serialize(requestBody);
        try
        {
            var response = await _httpClient.PostAsync(uri, new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) { Console.WriteLine($"Error AI suggestions: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"); return new List<string>(); }
            var responseContent = await response.Content.ReadAsStringAsync(); using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0 && cands[0].TryGetProperty("content", out var cont) && cont.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0 && parts[0].TryGetProperty("text", out var textEl))
            { var text = textEl.GetString(); var suggestions = new List<string>(); if (!string.IsNullOrWhiteSpace(text)) foreach (var line in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) { var cleaned = line.TrimStart('1', '2', '3', '4', '5', '.', '-', '*', ' ').Trim(); if (!string.IsNullOrWhiteSpace(cleaned)) suggestions.Add(cleaned); } return suggestions; }
            Console.WriteLine("AI suggestions response structure not as expected."); return new List<string>();
        }
        catch (Exception ex) { Console.WriteLine($"Exception AI suggestions: {ex.Message}"); return new List<string>(); }
    }

    public async Task<JsonResult> OnGetAISuggestionsAsync() => new JsonResult(await GetAISuggestionsAsync());

    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteUserAsync(int id)
    {
        var currentUserName = User.Identity?.Name; var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == currentUserName);
        if (adminUser == null || adminUser.Role != "Admin") return new JsonResult(new { success = false, message = "Unauthorized." }) { StatusCode = 403 };
        var userToDelete = await _db.Users.FindAsync(id); if (userToDelete == null) return new JsonResult(new { success = false, message = "User not found." }) { StatusCode = 404 };
        if (userToDelete.Username == currentUserName) return new JsonResult(new { success = false, message = "Cannot delete self." }) { StatusCode = 400 };
        try { _db.Users.Remove(userToDelete); await _db.SaveChangesAsync(); return new JsonResult(new { success = true, message = $"User '{userToDelete.Username}' deleted." }); }
        catch (Exception ex) { Console.WriteLine($"Error deleting user: {ex.Message}"); return new JsonResult(new { success = false, message = "Error deleting user." }) { StatusCode = 500 }; }
    }

    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostSaveUserAsync([FromBody] UserViewModel model)
    {
        string originalSubmittedPassword = model.Password;

        if (model.Id != 0 && string.IsNullOrWhiteSpace(model.Password))
        {
            var existingUserForPassword = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == model.Id);
            if (existingUserForPassword != null)
            {
                model.Password = existingUserForPassword.Password; // Populate with existing PLAIN TEXT password
            }
            else
            {
                ModelState.AddModelError("", "User not found when attempting to preserve password during edit.");
            }
        }

        if (!ModelState.IsValid)
        {
            var errorDictionary = ModelState
                .Where(kvp => kvp.Value != null && kvp.Value.Errors.Any() && !string.IsNullOrEmpty(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
            return new JsonResult(new { success = false, message = "Validation failed. Please check fields.", errors = errorDictionary }) { StatusCode = 400 };
        }

        var currentUserName = User.Identity?.Name;
        var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == currentUserName);
        if (adminUser == null || adminUser.Role != "Admin")
            return new JsonResult(new { success = false, message = "Unauthorized action." }) { StatusCode = 403 };

        if (await _db.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
            return new JsonResult(new { success = false, message = $"Username taken.", errors = new Dictionary<string, string[]> { { nameof(UserViewModel.Username), new[] { $"Username '{model.Username}' is already taken." } } } }) { StatusCode = 400 };

        if (!string.IsNullOrWhiteSpace(model.Email) && await _db.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
            return new JsonResult(new { success = false, message = $"Email taken.", errors = new Dictionary<string, string[]> { { nameof(UserViewModel.Email), new[] { $"Email '{model.Email}' is already taken." } } } }) { StatusCode = 400 };

        if (model.Id == 0) // Create new user
        {
            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = originalSubmittedPassword, // Store PLAIN TEXT password
                Role = model.Role
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, message = "User created successfully.", user = new { newUser.Id, newUser.Username, newUser.Email, newUser.Role } });
        }
        else // Update existing user
        {
            var userToUpdate = await _db.Users.FindAsync(model.Id);
            if (userToUpdate == null)
                return new JsonResult(new { success = false, message = "User not found." }) { StatusCode = 404 };

            userToUpdate.Username = model.Username;
            userToUpdate.Email = model.Email;
            userToUpdate.Role = model.Role;

            if (!string.IsNullOrWhiteSpace(originalSubmittedPassword))
            {
                userToUpdate.Password = originalSubmittedPassword; // Store new PLAIN TEXT password
            }
            // Else: originalSubmittedPassword was blank, so userToUpdate.Password (the existing plain text password) is NOT changed.

            _db.Users.Update(userToUpdate);
            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, message = "User updated successfully.", user = new { userToUpdate.Id, userToUpdate.Username, userToUpdate.Email, userToUpdate.Role } });
        }
    }
}

// UserViewModel
public class UserViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    // StringLength now applies to the plain text password.
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters long.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; }
}