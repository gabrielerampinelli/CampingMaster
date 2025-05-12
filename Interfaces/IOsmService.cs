using CampingMaster.Models;

namespace CampingMaster.Interfaces
{
    public interface IOsmService
    {
        Task<IEnumerable<Camping>> SearchCampingsAsync(string query);
    }
}