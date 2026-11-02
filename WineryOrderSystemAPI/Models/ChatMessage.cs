using System;
using System.Collections.Generic;

namespace WineryOrderSystemAPI.Models;

public partial class ChatMessage
{
    public int MessageId { get; set; }

    public int SessionId { get; set; }

    public string MessageText { get; set; } = null!;

    public bool IsBot { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;
}
