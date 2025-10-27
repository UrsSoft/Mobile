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
                .ForMember(dest => dest.SiteId, opt => opt.MapFrom(src => src.SiteId ?? 0)) // Default to 0 if no site
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User.Phone))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.User.CreatedDate))
                .ForMember(dest => dest.SiteName, opt => opt.MapFrom(src => src.Site != null ? src.Site.Name : "Atanmamýþ"));

            // User to UserDto mapping
            CreateMap<User, UserDto>();
            
            // Brand mappings
            CreateMap<Brand, BrandDto>();
            CreateMap<CreateBrandDto, Brand>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
            
            // Site to SiteDto mapping with brands
            CreateMap<Site, SiteDto>()
                .ForMember(dest => dest.Employees, opt => opt.MapFrom(src => src.Employees))
                .ForMember(dest => dest.Brands, opt => opt.MapFrom(src => src.SiteBrands.Select(sb => sb.Brand)));

            // CreateSiteDto to Site mapping
            CreateMap<CreateSiteDto, Site>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.SiteBrands, opt => opt.Ignore()); // Handle manually in controller
            
            // Request to RequestDto mapping with flattened properties
            CreateMap<Request, RequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.User.FullName))
                .ForMember(dest => dest.SiteId, opt => opt.MapFrom(src => src.Employee.SiteId ?? 0))
                .ForMember(dest => dest.SiteName, opt => opt.MapFrom(src => src.Employee.Site != null ? src.Employee.Site.Name : "Atanmamýþ"))
                .ForMember(dest => dest.Offers, opt => opt.MapFrom(src => src.Offers))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.RequestDate));
            
            // Request to EmployeeRequestDto mapping (for employee users - excludes offers)
            CreateMap<Request, EmployeeRequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.User.FullName))
                .ForMember(dest => dest.SiteId, opt => opt.MapFrom(src => src.Employee.SiteId ?? 0))
                .ForMember(dest => dest.SiteName, opt => opt.MapFrom(src => src.Employee.Site != null ? src.Employee.Site.Name : "Atanmamýþ"))
                .ForMember(dest => dest.OfferCount, opt => opt.MapFrom(src => src.Offers.Count))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.RequestDate));
            
            // Offer to OfferDto mapping with flattened properties (updated for new structure)
            CreateMap<Offer, OfferDto>()
                .ForMember(dest => dest.RequestTitle, opt => opt.MapFrom(src => src.Request.ProductDescription))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.User.FullName))
                .ForMember(dest => dest.SupplierEmail, opt => opt.MapFrom(src => src.Supplier.User.Email))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Supplier.CompanyName));
            
            // CreateOfferDto to Offer mapping
            CreateMap<CreateOfferDto, Offer>();
            
            // Supplier to SupplierDto mapping with User navigation property
            CreateMap<Supplier, SupplierDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // Notification mappings
            CreateMap<Notification, NotificationDto>();
            CreateMap<CreateNotificationDto, Notification>();
        }
    }
}