namespace MemberOrgApi.Constants;

public static class ActivityTypes
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string LoginFailed = "LoginFailed";
    public const string Registration = "Registration";
    public const string PasswordChange = "PasswordChange";
    public const string PasswordReset = "PasswordReset";
    public const string EmailVerification = "EmailVerification";
    
    public const string ProfileUpdate = "ProfileUpdate";
    public const string AddressUpdate = "AddressUpdate";
    public const string ContactUpdate = "ContactUpdate";
    public const string ProfileView = "ProfileView";
    
    public const string MembershipTierChange = "MembershipTierChange";
    public const string SubscriptionCreated = "SubscriptionCreated";
    public const string SubscriptionUpdated = "SubscriptionUpdated";
    public const string SubscriptionCanceled = "SubscriptionCanceled";
    public const string PaymentSucceeded = "PaymentSucceeded";
    public const string PaymentFailed = "PaymentFailed";
    public const string InvoiceGenerated = "InvoiceGenerated";
    
    public const string EventRegistration = "EventRegistration";
    public const string EventCancellation = "EventCancellation";
    public const string EventAttendance = "EventAttendance";
    public const string DonationMade = "DonationMade";
    
    public const string RoleChanged = "RoleChanged";
    public const string AccountActivated = "AccountActivated";
    public const string AccountDeactivated = "AccountDeactivated";
    public const string UserDeleted = "UserDeleted";
    public const string BulkOperation = "BulkOperation";
    public const string DataExported = "DataExported";
    
    public const string EmailSent = "EmailSent";
    public const string EmailOpened = "EmailOpened";
    public const string EmailClicked = "EmailClicked";
    public const string DocumentDownloaded = "DocumentDownloaded";
}

public static class ActivityCategories
{
    public const string Authentication = "Authentication";
    public const string Profile = "Profile";
    public const string Subscription = "Subscription";
    public const string Engagement = "Engagement";
    public const string Administration = "Administration";
    public const string Communication = "Communication";
}