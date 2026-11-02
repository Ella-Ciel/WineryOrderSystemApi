namespace WineryOrderSystemAPI.DTOs
{
    public class RegisterCostumerDto
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int RoleId
        {
            get => 2; set { }
        }
    }
}