using CampingMaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CampingMaster.Services
{
    public class CampingService
    {
        private readonly List<Camping> _campsites = new List<Camping>
        {
            new Camping { Id = 1, Name = "Mountain Retreat", Description = "A peaceful mountain campsite.", Latitude = 45.123, Longitude = -73.123 },
            new Camping { Id = 2, Name = "Lakeside Camp", Description = "Relax by the lake and enjoy nature.", Latitude = 46.567, Longitude = -72.345 },
            new Camping { Id = 3, Name = "Forest Hideaway", Description = "A secluded forest campsite.", Latitude = 44.567, Longitude = -74.234 }
        };

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of Earth in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Distance in km
        }

        private double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }

        public List<Camping> GetCampsitesNearby()
        {
            return new List<Camping>();
        }
    }
}
