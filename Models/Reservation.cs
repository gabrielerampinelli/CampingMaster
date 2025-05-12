using System.ComponentModel.DataAnnotations;

namespace CampingMaster.Models
{

    public class Reservation
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public long CampingId { get; set; }
        public int NumberOfPeople { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public Camping Camping { get; set; }
    }
}