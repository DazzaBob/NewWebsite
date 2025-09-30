using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Website.App.Security;

namespace Website.Pages.User
{
    public class SignUpModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(32, MinimumLength = 8, ErrorMessage = "Full name must be between 8 and 32 characters.")]
        [RegularExpression(@"^[A-Za-z\s\-']+$", ErrorMessage = "Enter a valid full name.")]
        public required string FullName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(128, MinimumLength = 5, ErrorMessage = "Email must be between 5 and 128 characters.")]
        public required string Email { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^02\d{7,10}$", ErrorMessage = "Enter a valid NZ mobile number starting with 02.")]
        [StringLength(13, MinimumLength = 9, ErrorMessage = "Phone number must be between 9 and 12 digits.")]
        public required string Phone { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(18, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 18 characters.")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        public string? ErrorMessage { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please select a valid address type.")]
        public int AddressTypeID { get; set; }

        [BindProperty(SupportsGet = false)]
        [ValidateNever]
        public string AddressSearch { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please select a valid address.")]
        public required string MapboxAddressJSON { get; set; }

        [ValidateNever]
        public SelectList AddressTypeOptions { get; set; } = new(Enumerable.Empty<SelectListItem>());

        [ViewData]
        public string? MapboxPublicToken { get; set; }

        public void OnGet()
        {
            MapboxPublicToken = App.Settings.MapboxToken;

            using App.Helper.Connection LocationConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);
            var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions(LocationConnection); // Load the address types for the dropdown
            AddressTypeOptions = list;
            ErrorMessage = error;
        }
        public IActionResult OnPost()
        {
            ModelState.Remove("AddressSearch");

            if (!ModelState.IsValid)
            {
                using App.Helper.Connection LocationConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);
                var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions(LocationConnection); // Load the address types for the dropdown
                AddressTypeOptions = list;
                ErrorMessage = error;

                // Repopulate AddressSearch if we still have MapboxAddressJSON
                if (!string.IsNullOrWhiteSpace(MapboxAddressJSON))
                {
                    try
                    {
                        var feature = JsonDocument.Parse(MapboxAddressJSON).RootElement;

                        if (feature.TryGetProperty("place_name", out var placeName) &&
                            placeName.ValueKind == JsonValueKind.String)
                        {
                            AddressSearch = placeName.GetString() ?? string.Empty;
                        }
                    }
                    catch
                    {
                        // Silent fail
                    }
                }

                return Page();
            }
            else
            {
                using App.Helper.Connection EntityConnection = App.Database.Shared.Connection(App.Database.Schema.Entities.Database);
                using App.Helper.Connection LocationConnection = App.Database.Shared.Connection(App.Database.Schema.Locations.Database);

                try
                {
                    bool emailExists = false;
                    bool phoneExists = false;

                    string emailCheckSql = "SELECT ID FROM USER WHERE EMAIL = @Email LIMIT 1";
                    Microsoft.Data.Sqlite.SqliteParameter[] emailParams =
                    [
                        new Microsoft.Data.Sqlite.SqliteParameter("@Email", Email.ToLowerInvariant() ?? (object)DBNull.Value)
                    ];
                    object? emailObj = EntityConnection.ExecuteScalar(emailCheckSql, emailParams);
                    emailExists = emailObj != null && emailObj != DBNull.Value && Convert.ToInt32(emailObj) > 0;

                    string phoneCheckSql = "SELECT ID FROM USER WHERE PHONE = @Phone LIMIT 1";
                    Microsoft.Data.Sqlite.SqliteParameter[] phoneParams =
                    [
                        new Microsoft.Data.Sqlite.SqliteParameter("@Phone", Phone ?? (object)DBNull.Value)
                    ];
                    object? phoneObj = EntityConnection.ExecuteScalar(phoneCheckSql, phoneParams);
                    phoneExists = phoneObj != null && phoneObj != DBNull.Value && Convert.ToInt32(phoneObj) > 0;

                    if (emailExists || phoneExists)
                    {
                        TempData["RecoverEmail"] = Email.ToLowerInvariant();
                        TempData["RecoverPhone"] = Phone;
                        return RedirectToPage("/User/Recover");
                    }

                    string hash = App.Helper.Shared.HashPassword(Password);
                    var CustomerRoleId = App.Database.Shared.GetScalar(EntityConnection, App.Database.Schema.Entities.Tables.Roles, "ID", "NAME='Customer'");

                    string sql = @"INSERT INTO USER (FULLNAME, EMAIL, PHONE, PASSWORDHASH, CREATEDOADATE, UPDATEDOADATE) 
                    VALUES (@FullName, @Email, @Phone, @PasswordHash, @Created, @Updated)";

                    Microsoft.Data.Sqlite.SqliteParameter[] parameters =
                    [
                        new Microsoft.Data.Sqlite.SqliteParameter("@FullName", FullName ?? (object)DBNull.Value),
                        new Microsoft.Data.Sqlite.SqliteParameter("@Email", Email.ToLowerInvariant() ?? (object)DBNull.Value),
                        new Microsoft.Data.Sqlite.SqliteParameter("@Phone", Phone ?? (object)DBNull.Value),
                        new Microsoft.Data.Sqlite.SqliteParameter("@PasswordHash", hash),
                        new Microsoft.Data.Sqlite.SqliteParameter("@Created", DateTime.Now.ToOADate()),
                        new Microsoft.Data.Sqlite.SqliteParameter("@Updated", DateTime.Now.ToOADate())
                    ];

                    EntityConnection.ExecuteNonQuery(sql, parameters);

                    int userId = 0;
                    DataTable DT = App.Database.Shared.GetDataTable(EntityConnection, "USER", $"EMAIL={App.Database.Shared.SafeReplace(Email.ToLowerInvariant())} AND PHONE='{Phone}'");
                    if (DT != null)
                    {
                        userId = Convert.ToInt32(DT.Rows[0]["ID"]);
                    }

                    sql = @"INSERT INTO USER_ROLES (USER_ID, ROLE_ID, GRANTEDOADATE, EXPIRATIONOADATE, REVOKEDOADATE, REVOKEDREASON, CREATEDBY_USER_ID, UPDATEDBY_USER_ID, ISACTIVE) 
                    VALUES (@UserId, @RoleId, @Granted, NULL, NULL, NULL, @CreatedBy, @UpdatedBy, 1);";

                    Microsoft.Data.Sqlite.SqliteParameter[] roleParams =
                    [
                        new Microsoft.Data.Sqlite.SqliteParameter("@UserId", userId),
                        new Microsoft.Data.Sqlite.SqliteParameter("@RoleId", CustomerRoleId), // "Customer"
                        new Microsoft.Data.Sqlite.SqliteParameter("@Granted", DateTime.Now.ToOADate()),
                        new Microsoft.Data.Sqlite.SqliteParameter("@CreatedBy", userId), // The user themselves, or could be 0/admin
                        new Microsoft.Data.Sqlite.SqliteParameter("@UpdatedBy", userId)  // Same as CreatedBy
                    ];
                    EntityConnection.ExecuteNonQuery(sql, roleParams);

                    // Set ClaimsPrincipal (Authinticate the user and sign them in)
                    HttpContext.SignInUser(userId);
                    try
                    {
                        // 1) Persist the address & enqueue zoning
                        long addressId = App.Validation.AddressValidation.SaveAddressAndGetId(MapboxAddressJSON, LocationConnection, EntityConnection, AddressTypeID, userId, true);
                        if (addressId <= 0)
                        {
                            ModelState.AddModelError(string.Empty, "Could not save address. Please check your input.");
                            return Page();
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Address error: " + ex.Message);
                        return Page();
                    }
                    // Yay! Registration successful.
                    return RedirectToPage("/User/Dashboard/Index");
                }
                catch (Exception ex)
                {
                    // Log the error and show a generic message
                    ErrorMessage = "An unexpected error occurred: " + ex.Message;

                    var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions(LocationConnection); // Load the address types for the dropdown
                    AddressTypeOptions = list;
                    ErrorMessage = error;

                    return Page();

                }
            }
        }

    }
}