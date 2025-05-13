namespace API.Models.DTOs
{
    public class MessageDTO
    {
        public Guid MessageID { get; set; }
        public required string SenderId { get; set; }
        public required string ReceiverId { get; set; }
        public required string Content { get; set; }
        public DateTime SentAt { get; set; }
        public required string SenderName { get; set; }
        public required string ReceiverName { get; set; }
    }

    public class SendMessageDto
    {
        public required string ReceiverId { get; set; }
        public required string Content { get; set; }
    }

    public class ChatHistoryDTO
    {
        public required string ChatId { get; set; }
        public List<MessageDTO>? Messages { get; set; }
    }
}
