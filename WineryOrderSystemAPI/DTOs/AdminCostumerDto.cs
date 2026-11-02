namespace WineryOrderSystemAPI.DTOs
{
    public class AdminCostumerDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RoleName { get; set; } = null!;
    }
}