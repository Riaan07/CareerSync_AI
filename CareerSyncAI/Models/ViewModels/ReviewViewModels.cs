using System.ComponentModel.DataAnnotations;

namespace CareerSyncAI.Models.ViewModels;

public class ReviewViewModel
{
    [Required, Range(1, 5, ErrorMessage = "Choose a rating from 1 to 5.")]
    public int Rating { get; set; }

    [Required, MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewsListViewModel
{
    public List<ReviewRow> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public ReviewViewModel? MyReview { get; set; }
}

public class ReviewRow
{
    public int ReviewId { get; set; }
    public string GraduateName { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}
