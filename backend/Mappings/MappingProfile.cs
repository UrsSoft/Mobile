using AutoMapper;
using SantiyeTalepApi.Models;
using SantiyeTalepApi.DTOs;

namespace SantiyeTalepApi.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Employee to EmployeeDto mapping with flattened properties
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.SiteId, opt => opt.MapFrom(src => src.SiteId))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.Phone))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.User.CreatedDate))
                .ForMember(dest => dest.SiteName, opt => opt.MapFrom(src => src.Site.Name));

            // User to UserDto mapping
            CreateMap<User, UserDto>();
            
            // Site to SiteDto mapping
            CreateMap<Site, SiteDto>();

            // CreateSiteDto to Site mapping
            CreateMap<CreateSiteDto, Site>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
            
            // Request to RequestDto mapping
            CreateMap<Request, RequestDto>();
            
            // Offer to OfferDto mapping
            CreateMap<Offer, OfferDto>();
            
            // Supplier to SupplierDto mapping with flattened properties
            CreateMap<Supplier, SupplierDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName))
                .ForMember(dest => dest.TaxNumber, opt => opt.MapFrom(src => src.TaxNumber))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.Phone))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.User.CreatedDate));
        }
    }
}