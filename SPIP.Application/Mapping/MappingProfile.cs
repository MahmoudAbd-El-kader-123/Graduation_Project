using AutoMapper;
using SPIP.Application.DTOs.User;
using SPIP.Domain.Entities;

namespace SPIP.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}
