namespace HuquqApi.Model
{
    public class Chat
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public List<Message> Messages { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdateTime { get; set; }

    }

}
