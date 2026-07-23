namespace SPIP.Domain.Constants;

public static class Permissions
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Update = "Users.Update";
        public const string Activate = "Users.Activate";
        public const string Deactivate = "Users.Deactivate";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
    }

    public static class Vendors
    {
        public const string View = "Vendors.View";
        public const string Create = "Vendors.Create";
        public const string Update = "Vendors.Update";
        public const string Delete = "Vendors.Delete";
    }

    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Update = "Products.Update";
        public const string Delete = "Products.Delete";
    }

    public static class POImports
    {
        public const string View = "POImports.View";
        public const string Import = "POImports.Import";
        public const string Delete = "POImports.Delete";
    }

    public static class Invoices
    {
        public const string Upload = "Invoices.Upload";
        public const string View = "Invoices.View";
        public const string Download = "Invoices.Download";
        public const string ViewAll = "Invoices.ViewAll";
    }

    public static class VendorMappings
    {
        public const string View = "VendorMappings.View";
        public const string Manage = "VendorMappings.Manage";
    }

    public static class Dashboard
    {
        public const string View = "Dashboard.View";
    }

    public static class Reports
    {
        public const string View = "Reports.View";
    }
}
