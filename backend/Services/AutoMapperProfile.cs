using AutoMapper;
using SantiyeTalepApi.DTOs;
using SantiyeTalepApi.Models;

namespace SantiyeTalepApi.Services
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<CreateEmployeeDto, User>();
            CreateMap<SupplierRegisterDto, User>();
            CreateMap<SupplierRegisterDto, Supplier>();
            
            CreateMap<Employee, EmployeeDto>();
            CreateMap<Supplier, SupplierDto>();
            CreateMap<Site, SiteDto>();
            CreateMap<CreateSiteDto, Site>();
            
            CreateMap<Request, RequestDto>()
                .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => src.Employee.User.FullName))
                .ForMember(dest => dest.SiteName, opt => opt.MapFrom(src => src.Site.Name));
            CreateMap<CreateRequestDto, Request>();
            
            CreateMap<Offer, OfferDto>()
                .ForMember(dest => dest.RequestTitle, opt => opt.MapFrom(src => src.Request.ProductDescription))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.User.FullName))
                .ForMember(dest => dest.SupplierEmail, opt => opt.MapFrom(src => src.Supplier.User.Email))
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Supplier.CompanyName));
            CreateMap<CreateOfferDto, Offer>();
        }
    }
}
