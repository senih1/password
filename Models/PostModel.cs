using System.ComponentModel.DataAnnotations;

namespace password.Models
{
    public class Post
    {
        public int Id { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Fullname { get; set; }
        public string? Bio { get; set; }
        [Required]
        public bool isPrivate { get; set; }
        public string? ProfilePictureUrl { get; set; }
        [Required]
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class Comments
    {
        public int Id { get; set; }
        [Required]
        public string Message { get; set; }
        [Required]
        public int UserId { get; set; }
        public string? Email { get; set; }
        [Required]
        public int PostId { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Username { get; set; }
        public DateTime CreatedDate { get; set; }

    }
    public class PostModel
    {
        public Post Post { get; set; }
        public List<Comments> Comments { get; set; }
    }
}
