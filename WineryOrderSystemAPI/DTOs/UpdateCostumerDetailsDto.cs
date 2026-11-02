namespace WineryOrderSystemAPI.DTOs
{
    public class UpdateCostumerDetailsDto
    {
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}