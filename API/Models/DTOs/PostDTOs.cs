﻿using API.Models.Entities;

namespace API.Models.DTOs
{
    public class CreatePostDTO
    {
        public string Content { get; set; } = null!;
        public string? MediaFile { get; set; }
        public string? MediaType { get; set; }
        public PostTargetType TargetType { get; set; } = PostTargetType.Friends;
        public List<Guid>? TargetGroupIds { get; set; }
    }    public class PostDTO
    {
        public Guid PostId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatarUrl { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }
        public PostTargetType TargetType { get; set; }
        public List<Guid>? TargetGroupIds { get; set; }
    }

    public class PostDetailDTO : PostDTO
    {
        public List<PostCommentDTO> Comments { get; set; } = new List<PostCommentDTO>();
        public List<PostLikeDTO> Likes { get; set; } = new List<PostLikeDTO>();
    }    public class CreateCommentDTO
    {
        public Guid PostId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class UpdatePostTargetDTO
    {
        public PostTargetType TargetType { get; set; }
        public List<Guid>? TargetGroupIds { get; set; }
    }

    public class PostCommentDTO
    {
        public Guid CommentId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string AuthorId { get; set; } = null!;
        public string AuthorName { get; set; } = null!;
        public string? AuthorAvatarUrl { get; set; }
    }

    public class PostLikeDTO
    {
        public Guid LikeId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class NewsFeedDTO
    {
        public List<PostDTO> Posts { get; set; } = new List<PostDTO>();
        public int TotalCount { get; set; }
        public bool HasMorePosts { get; set; }
    }
}