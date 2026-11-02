using WineryOrderSystemAPI.DTOs;

namespace WineryOrderSystemAPI.Services
{
    public interface IChatbotService
    {
        Task<ChatResponseDto> ProcessUserMessageAsync(ChatPromptDto request);
    }
}