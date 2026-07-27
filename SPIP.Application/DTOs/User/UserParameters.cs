using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.User;

public class UserParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
