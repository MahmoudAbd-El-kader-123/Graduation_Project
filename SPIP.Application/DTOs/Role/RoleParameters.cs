using SPIP.Shared.Pagination;

namespace SPIP.Application.DTOs.Role;

public class RoleParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }
}
