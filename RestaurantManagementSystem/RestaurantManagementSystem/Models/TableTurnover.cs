using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class TableTurnover
    {
        public int Id { get; set; }

        [Required]
        public int TableId { get; set; }

        [Display(Name = "Reservation ID")]
        public int? ReservationId { get; set; }

        [Display(Name = "Waitlist ID")]
        public int? WaitlistId { get; set; }

        [Required]
        [Display(Name = "Guest Name")]
        public string GuestName { get; set; }

        [Required]
        [Display(Name = "Party Size")]
        public int PartySize { get; set; }

        [Display(Name = "Seated At")]
        public DateTime SeatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Started Service At")]
        public DateTime? StartedServiceAt { get; set; }

        [Display(Name = "Completed At")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Departed At")]
        public DateTime? DepartedAt { get; set; }

        [Display(Name = "Status")]
        public TableTurnoverStatus Status { get; set; } = TableTurnoverStatus.Seated;

        [Display(Name = "Special Notes")]
        public string Notes { get; set; }

        [Display(Name = "Total Duration")]
        public TimeSpan? Duration 
        { 
            get 
            {
                if (DepartedAt.HasValue && SeatedAt != null)
                {
                    return DepartedAt.Value - SeatedAt;
                }
                return null;
            }
        }

        [Display(Name = "Target Turn Time (minutes)")]
        public int TargetTurnTimeMinutes { get; set; } = 90; // Default 90 minutes

        public bool IsOverTargetTime
        {
            get
            {
                if (Status != TableTurnoverStatus.Completed && Status != TableTurnoverStatus.Departed)
                {
                    var currentDuration = DateTime.Now - SeatedAt;
                    return currentDuration.TotalMinutes > TargetTurnTimeMinutes;
                }
                return false;
            }
        }

        // Navigation properties
        public virtual Table Table { get; set; }
        public virtual Reservation Reservation { get; set; }
        public virtual WaitlistEntry WaitlistEntry { get; set; }
    }

    public enum TableTurnoverStatus
    {
        Seated = 0,        // Just seated
        InService = 1,     // Order taken, food/drinks being served
        CheckRequested = 2, // Bill requested
        Paid = 3,          // Bill paid
        Completed = 4,     // Service completed
        Departed = 5       // Guests have left
    }
}
