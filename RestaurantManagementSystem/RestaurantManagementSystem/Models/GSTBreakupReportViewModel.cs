using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class GSTBreakupReportViewModel
    {
        public GSTBreakupReportFilter Filter { get; set; } = new GSTBreakupReportFilter();
        public GSTBreakupReportSummary Summary { get; set; } = new GSTBreakupReportSummary();
        public List<GSTBreakupReportRow> Rows { get; set; } = new List<GSTBreakupReportRow>();
    }

    public class GSTBreakupReportFilter
    {
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; } = DateTime.Today;

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; } = DateTime.Today;
    }

    public class GSTBreakupReportSummary
    {
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalCGST { get; set; }
        public decimal TotalSGST { get; set; }
        public decimal TotalGST => TotalCGST + TotalSGST;
        public decimal NetAmount { get; set; } // Taxable - Discount + GST (or final invoice total sum)

        public decimal AverageTaxablePerInvoice { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageGSTPerInvoice { get; set; }
    }

    public class GSTBreakupReportRow
    {
        public DateTime PaymentDate { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; } // Amount excluding GST after discount
        public decimal DiscountAmount { get; set; }
        public decimal CGSTPercentage { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTPercentage { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal TotalGST => CGSTAmount + SGSTAmount;
        public decimal InvoiceTotal { get; set; } // TaxableValue + TotalGST

        public string PaymentDateFormatted => PaymentDate.ToString("yyyy-MM-dd HH:mm");
    }
}
