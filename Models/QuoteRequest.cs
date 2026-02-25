namespace NineArchTours.Models
{
    public class QuoteRequest
    {
        public string ClientName { get; set; } = string.Empty;
        public string ArrivalDate { get; set; } = string.Empty;
        public string DepartureDate { get; set; } = string.Empty;
        public string TourTitle { get; set; } = string.Empty;
        public string HotelCategory { get; set; } = "5 Star";
        public string MealPlan { get; set; } = "BB";
        public int NumberOfNights { get; set; }
        public List<DayPlan> Days { get; set; } = new();
        public int SelectedVehicleId { get; set; }
        public decimal TotalCost { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public List<string> Inclusions { get; set; } = new();
        public List<string> Exclusions { get; set; } = new();
        public PackageOption? Option1 { get; set; }
        public PackageOption? Option2 { get; set; }
    }

    public class DayPlan
    {
        public int DayNumber { get; set; }
        public string ItineraryDescription { get; set; } = string.Empty;
        public string Highlights { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public List<string> LocationImages { get; set; } = new();
        public string HotelName { get; set; } = string.Empty;
        public string HotelDescription { get; set; } = string.Empty;
        public string HotelStarRating { get; set; } = string.Empty;
        public string HotelLocation { get; set; } = string.Empty;
        public List<string> HotelImages { get; set; } = new();
        public string Meals { get; set; } = "Breakfast included";
    }

    public class PackageOption
    {
        public string Title { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public List<PackageOptionHotel> Hotels { get; set; } = new();
    }

    public class PackageOptionHotel
    {
        public string HotelName { get; set; } = string.Empty;
        public string HotelDescription { get; set; } = string.Empty;
        public string HotelStarRating { get; set; } = string.Empty;
        public string HotelLocation { get; set; } = string.Empty;
        public string FoodType { get; set; } = string.Empty;
        public List<string> HotelImages { get; set; } = new();
    }
}
