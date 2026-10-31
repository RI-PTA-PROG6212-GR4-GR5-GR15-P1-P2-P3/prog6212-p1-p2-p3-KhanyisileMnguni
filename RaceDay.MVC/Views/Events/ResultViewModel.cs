using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ResultViewModel
    {
        public int EnrolmentId { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required]
        public string Status { get; set; } = "Finished";
    }
}
