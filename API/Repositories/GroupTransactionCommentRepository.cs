using API.Data;
using API.Models.DTOs;
using API.Models.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class GroupTransactionCommentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GroupTransactionCommentRepository(
            ApplicationDbContext context,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the current user ID from the HTTP context
        /// </summary>
        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value ??
                   throw new UnauthorizedAccessException("User is not authenticated");
        }

        /// <summary>
        /// Get all comments for a group transaction
        /// </summary>
        public async Task<IEnumerable<GroupTransactionCommentDTO>> GetCommentsByTransactionIdAsync(Guid transactionId)
        {
            var comments = await _context.GroupTransactionComments
                .Include(c => c.User)
                .Where(c => c.GroupTransactionId == transactionId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<GroupTransactionCommentDTO>>(comments);
        }

        /// <summary>
        /// Add a new comment to a group transaction
        /// </summary>
        public async Task<GroupTransactionCommentDTO> AddCommentAsync(CreateGroupTransactionCommentDTO dto)
        {
            // Check if transaction exists
            var transaction = await _context.GroupTransactions
                .FirstOrDefaultAsync(t => t.GroupTransactionID == dto.GroupTransactionId);

            if (transaction == null)
            {
                throw new KeyNotFoundException($"Group transaction with ID {dto.GroupTransactionId} not found");
            }

            var userId = GetCurrentUserId();

            // Create the comment entity
            var comment = _mapper.Map<GroupTransactionComment>(dto);
            comment.UserId = userId;
            comment.CreatedAt = DateTime.UtcNow;

            await _context.GroupTransactionComments.AddAsync(comment);
            await _context.SaveChangesAsync();

            // Reload the comment with user data for mapping
            var createdComment = await _context.GroupTransactionComments
                .Include(c => c.User)
                .FirstAsync(c => c.CommentId == comment.CommentId);

            return _mapper.Map<GroupTransactionCommentDTO>(createdComment);
        }

        /// <summary>
        /// Update an existing comment
        /// </summary>
        public async Task<GroupTransactionCommentDTO> UpdateCommentAsync(UpdateGroupTransactionCommentDTO dto)
        {
            var comment = await _context.GroupTransactionComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == dto.CommentId);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {dto.CommentId} not found");
            }

            var userId = GetCurrentUserId();
            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this comment");
            }

            // Update properties
            _mapper.Map(dto, comment);
            comment.UpdatedAt = DateTime.UtcNow;

            _context.GroupTransactionComments.Update(comment);
            await _context.SaveChangesAsync();

            return _mapper.Map<GroupTransactionCommentDTO>(comment);
        }

        /// <summary>
        /// Delete a comment
        /// </summary>
        public async Task DeleteCommentAsync(Guid commentId)
        {
            var comment = await _context.GroupTransactionComments
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment with ID {commentId} not found");
            }

            var userId = GetCurrentUserId();
            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this comment");
            }

            _context.GroupTransactionComments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}