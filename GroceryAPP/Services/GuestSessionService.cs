using System;
using System.Threading.Tasks;

namespace GroceryApp.Services;

public class GuestSessionService
{
    private const string GuestMobileKey = "guest_mobile_number";

    public async Task SaveGuestMobileAsync(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            return;
        }

        var normalized = NormalizeMobile(mobileNumber);
        await SecureStorage.Default.SetAsync(GuestMobileKey, normalized);
    }

    public async Task<string> GetGuestMobileAsync()
    {
        var value = await SecureStorage.Default.GetAsync(GuestMobileKey);
        return NormalizeMobile(value);
    }

    public void ClearGuestMobile()
    {
        SecureStorage.Default.Remove(GuestMobileKey);
    }

    private static string NormalizeMobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim();
    }
}
