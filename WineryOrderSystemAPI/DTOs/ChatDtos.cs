namespace WineryOrderSystemAPI.DTOs
{
    public class ChatPromptDto
    {
        public int UserId { get; set; }
        public int? SessionId { get; set; } // Send null or 0 to start a new chat session
        public string Prompt { get; set; } = string.Empty;
    }

    public class ChatResponseDto
    {
        public int SessionId { get; set; }
        public string Response { get; set; } = string.Empty;
        public bool IsFromFaq { get; set; }
    }

    public class FaqCreateDto
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}