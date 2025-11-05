namespace Clean.Application.Dtos.Filters;


public class PayrollRecordFilter
{
        public int? EmployeeId { get; set; }           // Optional: filter by employee
        public DateOnly? FromDate { get; set; }        // Start of period filter
        public DateOnly? ToDate { get; set; }          // End of period filter
        public decimal? MinNetPay { get; set; }        // Optional: filter by pay range
        public decimal? MaxNetPay { get; set; }
}


