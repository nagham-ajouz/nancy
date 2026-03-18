using AutoMapper;
using FleetService.Application.DTOs.Driver;
using FleetService.Application.DTOs.Vehicle;
using FleetService.Domain.Entities;

namespace FleetService.Application.Mapping;

public class FleetMappingProfile : Profile
{
    public FleetMappingProfile()
    {
        // Vehicle → VehicleDto
        // PlateNumber is a value object — map its .Value property to a plain string
        CreateMap<Vehicle, VehicleDto>()
            .ForMember(dest => dest.PlateNumber,
                opt  => opt.MapFrom(src => src.PlateNumber.Value))
            .ForMember(dest => dest.Type,
                opt  => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.Status,
                opt  => opt.MapFrom(src => src.Status.ToString()));

        // Driver → DriverDto
        // LicenseNumber is a value object — same pattern
        CreateMap<Driver, DriverDto>()
            .ForMember(dest => dest.LicenseNumber,
                opt  => opt.MapFrom(src => src.LicenseNumber.Value))
            .ForMember(dest => dest.Status,
                opt  => opt.MapFrom(src => src.Status.ToString()));
    }
}