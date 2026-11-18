using Capstone.Identity.API.Common;
using Capstone.Identity.API.Dtos.Response;

namespace Capstone.Identity.API.Services
{
    public interface IPlaceholderService
    {
        Task<Result<MergePreviewDto>> PreviewMergeAsync(Guid placeholderUserId, Guid targetUserId);
        Task<Result<bool>> MergeAsync(Guid placeholderUserId, Guid targetUserId, Guid actingAdminId);
        Task<Result<bool>> DeletePlaceholderAsync(Guid placeholderUserId, Guid actingAdminId);
    }
}