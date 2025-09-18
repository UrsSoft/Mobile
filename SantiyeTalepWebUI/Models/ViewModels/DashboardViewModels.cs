using SantiyeTalepWebUI.Models.DTOs;

namespace SantiyeTalepWebUI.Models.ViewModels
{
    public class DashboardViewModel
    {
        public UserDto CurrentUser { get; set; } = null!;
        public DashboardStats Stats { get; set; } = new();
        public List<RequestDto> RecentRequests { get; set; } = new();
        public List<OfferDto> RecentOffers { get; set; } = new();
    }

    public class DashboardStats
    {
        // Admin Stats
        public int TotalUsers { get; set; }
        public int TotalSites { get; set; }
        public int PendingSuppliers { get; set; }
        public int TotalRequests { get; set; }

        // Employee Stats
        public int MyRequests { get; set; }
        public int OpenRequests { get; set; }
        public int CompletedRequests { get; set; }

        // Supplier Stats
        public int MyOffers { get; set; }
        public int PendingOffers { get; set; }
        public int ApprovedOffers { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<SupplierDto> PendingSuppliers { get; set; } = new();
        public List<RequestDto> RecentRequests { get; set; } = new();
        public List<SiteDto> Sites { get; set; } = new();
        public List<EmployeeDto> Employees { get; set; } = new();
    }

    public class EmployeeDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<EmployeeRequestDto> MyRequests { get; set; } = new(); // Updated to use EmployeeRequestDto
        public SiteDto? MySite { get; set; }
    }

    public class SupplierDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public List<OfferDto> MyOffers { get; set; } = new();
        public List<RequestDto> AvailableRequests { get; set; } = new();
        public SupplierDto? MyProfile { get; set; }
    }
}