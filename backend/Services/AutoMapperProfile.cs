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
            
            CreateMap<Request, RequestDto>();
            CreateMap<CreateRequestDto, Request>();
            
            CreateMap<Offer, OfferDto>();
            CreateMap<CreateOfferDto, Offer>();
        }
    }
}
