namespace RaceDay.MVC.Models
{
    public class CategoryViewModel
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = "";
        public double DistanceKM { get; set; }
        public int MaxParticipants { get; set; }
        public decimal EntryFee { get; set; }
    }
}