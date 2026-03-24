using AutoMapper;
using NotificationService.Domain.Entities;
using NotificationService.Application.DTOs;

namespace NotificationService.Application.Mapping;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationDto>();
    }
}