using AutoMapper;
using TripService.Application.DTOs;
using TripService.Application.DTOs.Trip;
using TripService.Application.DTOs.TripLog;
using TripService.Domain.Entities;

namespace TripService.Application.Mapping;

public class TripMappingProfile : Profile
{
    public TripMappingProfile()
    {
        // Trip → TripDto
        // Location is a value object with nested properties
        CreateMap<Trip, TripDto>()
            .ForMember(dest => dest.Status,
                opt  => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.StartLocation,
                opt  => opt.MapFrom(src => src.StartLocation))
            .ForMember(dest => dest.EndLocation,
                opt  => opt.MapFrom(src => src.EndLocation))
            .ForMember(dest => dest.CostAmount,
                opt  => opt.MapFrom(src => src.Cost != null ? src.Cost.Amount : (decimal?)null))
            .ForMember(dest => dest.CostCurrency,
                opt  => opt.MapFrom(src => src.Cost != null ? src.Cost.Currency : null));

        // Location value object → LocationDto
        CreateMap<Shared.ValueObjects.Location, LocationDto>();

        // TripLog → TripLogDto
        // Location nested properties mapped to flat properties
        CreateMap<TripLog, TripLogDto>()
            .ForMember(dest => dest.Latitude,
                opt  => opt.MapFrom(src => src.Location.Latitude))
            .ForMember(dest => dest.Longitude,
                opt  => opt.MapFrom(src => src.Location.Longitude))
            .ForMember(dest => dest.Address,
                opt  => opt.MapFrom(src => src.Location.Address));
    }
}