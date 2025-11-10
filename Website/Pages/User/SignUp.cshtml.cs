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
        private readonly string EntitiesSchema = App.Database.Schema.Entities.Name;

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
        public required string HJSON { get; set; }

        [ValidateNever]
        public SelectList AddressTypeOptions { get; set; } = new(Enumerable.Empty<SelectListItem>());

        [ViewData]
        public string? MapboxPublicToken { get; set; }

        public void OnGet()
        {
            MapboxPublicToken = App.Settings.MapboxToken;
            var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions();
            AddressTypeOptions = list;
            ErrorMessage = error;
        }

        public IActionResult OnPost()
        {
            ModelState.Remove("AddressSearch");

            if (!ModelState.IsValid)
            {
                var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions();
                AddressTypeOptions = list;
                ErrorMessage = error;

                if (!string.IsNullOrWhiteSpace(HJSON))
                {
                    try
                    {
                        var feature = JsonDocument.Parse(HJSON).RootElement;
                        if (feature.TryGetProperty("place_name", out var placeName) &&
                            placeName.ValueKind == JsonValueKind.String)
                        {
                            AddressSearch = placeName.GetString() ?? string.Empty;
                        }
                    }
                    catch { }
                }

                return Page();
            }
            else
            {
                try
                {
                    using DataTable DT = App.Database.DataAccessManager.GetDataTable(
                        EntitiesSchema,
                        App.Database.Schema.Entities.Tables.Users,
                        $"(EMAIL={App.Database.Shared.Sanitize(Email.ToLowerInvariant(), true, true)}) OR (PHONE={App.Database.Shared.Sanitize(Phone.ToLowerInvariant(), true, true)})");

                    if (DT.Rows.Count > 0)
                    {
                        TempData["RecoverEmail"] = Email.ToLowerInvariant();
                        TempData["RecoverPhone"] = Phone;
                        return RedirectToPage("/User/Recover");
                    }

                    string hash = App.Helper.Shared.HashPassword(Password);

                    object? customerRoleObj = App.Database.DataAccessManager.GetScalar(
                        EntitiesSchema,
                        App.Database.Schema.Entities.Tables.Roles,
                        "ID",
                        "NAME='Customer'");

                    if (customerRoleObj == null)
                        throw new InvalidOperationException("Role 'Customer' not found.");

                    int CustomerRoleId = Convert.ToInt32(customerRoleObj);

                    string sql = @$"INSERT INTO {App.Database.Schema.Entities.Tables.Users}
                    (FULLNAME, EMAIL, PHONE, PASSWORDHASH, CREATEDOADATE, UPDATEDOADATE)
                    VALUES (@FullName, @Email, @Phone, @PasswordHash, @Created, @Updated)
                    RETURNING ID;";

                    Npgsql.NpgsqlParameter[] parameters =
                    [
                        new Npgsql.NpgsqlParameter("@FullName", FullName ?? (object)DBNull.Value),
                        new Npgsql.NpgsqlParameter("@Email", Email.ToLowerInvariant() ?? (object)DBNull.Value),
                        new Npgsql.NpgsqlParameter("@Phone", Phone ?? (object)DBNull.Value),
                        new Npgsql.NpgsqlParameter("@PasswordHash", hash),
                        new Npgsql.NpgsqlParameter("@Created", DateTime.UtcNow.ToOADate()),
                        new Npgsql.NpgsqlParameter("@Updated", DateTime.UtcNow.ToOADate())
                    ];

                    object? newIdObj = App.Database.DataAccessManager.ExecuteScalar(EntitiesSchema, sql, parameters);
                    if (newIdObj == null) throw new InvalidOperationException("User insert failed to return ID.");
                    int userId = Convert.ToInt32(newIdObj);

                    sql = @$"INSERT INTO {App.Database.Schema.Entities.Tables.UserRoles}
                    (USER_ID, ROLE_ID, GRANTEDOADATE, EXPIRATIONOADATE, REVOKEDOADATE, REVOKEDREASON, CREATEDBY_USER_ID, UPDATEDBY_USER_ID, ISACTIVE)
                    VALUES (@UserId, @RoleId, @Granted, NULL, NULL, NULL, @CreatedBy, @UpdatedBy, true);";

                    Npgsql.NpgsqlParameter[] roleParams =
                    [
                        new Npgsql.NpgsqlParameter("@UserId", userId),
                        new Npgsql.NpgsqlParameter("@RoleId", CustomerRoleId),
                        new Npgsql.NpgsqlParameter("@Granted", DateTime.UtcNow.ToOADate()),
                        new Npgsql.NpgsqlParameter("@CreatedBy", userId),
                        new Npgsql.NpgsqlParameter("@UpdatedBy", userId)
                    ];

                    _ = App.Database.DataAccessManager.ExecuteNonQuery(EntitiesSchema, sql, roleParams);

                    HttpContext.SignInUser(userId);

                    try
                    {
                        long addressId = App.Validation.AddressValidation.SaveAddressAndGetId(HJSON, AddressTypeID, userId, true);
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

                    return RedirectToPage("/Home/Index");
                }
                catch (Exception ex)
                {
                    ErrorMessage = "An unexpected error occurred: " + ex.Message;

                    var (list, error) = App.Helper.Table.AddressType.GetAddressTypeOptions();
                    AddressTypeOptions = list;
                    ErrorMessage = error;

                    return Page();
                }
            }
        }
    }
}