namespace MemberOrgApi.DTOs;

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public List<string>? DietaryRestrictions { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class RecordCheckPaymentRequest
{
    public string MembershipTier { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime StartDate { get; set; }
}

public class AdminStats
{
    // Basic Counts
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisPeriod { get; set; }
    public int ChurnedUsersThisPeriod { get; set; }

    // Financial
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualRecurringRevenue { get; set; }
    public int ActiveSubscriptions { get; set; }
    public Dictionary<string, decimal> RevenueByTier { get; set; } = new();
    public decimal RevenueGrowthRate { get; set; } // Percentage
    public int CancelledSubscriptions { get; set; }

    // Events
    public int TotalEventsHeld { get; set; }
    public decimal AverageRsvpResponseRate { get; set; } // Percentage
    public decimal AverageAttendanceRate { get; set; } // Percentage
    public decimal AverageAttendeesPerEvent { get; set; }
    public decimal AttendanceTrend { get; set; } // Percentage change

    // Demographics & Engagement
    public Dictionary<string, int> AgeDistribution { get; set; } = new();
    public decimal AverageEventsPerMember { get; set; }
    public List<TopEngagedMemberDto> TopEngagedMembers { get; set; } = new();

    // Metadata
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
}

public class TopEngagedMemberDto
{
    public string Name { get; set; } = string.Empty;
    public int EventsAttended { get; set; }
}

public class AdminCreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Role { get; set; } // defaults to Member
    public bool? IsActive { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}
