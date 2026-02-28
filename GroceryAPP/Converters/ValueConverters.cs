using System.Globalization;
using System.IO;

namespace GroceryApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Colors.Green : Colors.Red;
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? true : false;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal decimalValue)
        {
            return $"₹{decimalValue:F2}";
        }
        return "₹0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string strValue)
        {
            return strValue?.ToLower() switch
            {
                "pending" => Colors.Orange,
                "confirmed" => Colors.Blue,
                "shipped" => Colors.Purple,
                "delivered" => Colors.Green,
                "cancelled" => Colors.Red,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IsNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string image URL (http/https, data:image base64, or local resource name) to an ImageSource.
/// MAUI's Image control does not natively support data: URIs, so this converter handles all three cases.
/// </summary>
public class Base64ImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
            return null;

        // data:image/jpeg;base64,... — decode bytes and stream
        if (url.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = url.IndexOf(',');
            if (commaIndex < 0) return null;
            try
            {
                var base64 = url.Substring(commaIndex + 1);
                var bytes = System.Convert.FromBase64String(base64);
                return ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch
            {
                return null;
            }
        }

        // Absolute URI (http/https/etc.)
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ImageSource.FromUri(uri);

        // Local resource / app-bundle file (e.g. "dotnet_bot.png")
        return ImageSource.FromFile(url);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Returns a pastel background Color for an order-status badge.</summary>
public class StatusToBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as string)?.ToLower() switch
        {
            "pending"   => Color.FromArgb("#FFF3CD"),
            "confirmed" => Color.FromArgb("#D1ECF1"),
            "shipped"   => Color.FromArgb("#E2D9F3"),
            "delivered" => Color.FromArgb("#D4EDDA"),
            "cancelled" => Color.FromArgb("#F8D7DA"),
            _           => Color.FromArgb("#F5F5F5")
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CategoryToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string categoryName)
        {
            var lowerName = categoryName.ToLower();
            
            // Match category names to appropriate emojis
            if (lowerName.Contains("sabji") || lowerName.Contains("vegetable") || lowerName.Contains("सब्जी"))
                return "🥬";
            if (lowerName.Contains("cake") || lowerName.Contains("केक") || lowerName.Contains("bakery"))
                return "🎂";
            if (lowerName.Contains("fruit") || lowerName.Contains("फल"))
                return "🍎";
            if (lowerName.Contains("dairy") || lowerName.Contains("milk") || lowerName.Contains("दूध"))
                return "🥛";
            if (lowerName.Contains("meat") || lowerName.Contains("chicken") || lowerName.Contains("मांस"))
                return "🍗";
            if (lowerName.Contains("grocery") || lowerName.Contains("किराना"))
                return "🛒";
            if (lowerName.Contains("snack") || lowerName.Contains("नाश्ता"))
                return "🍿";
            if (lowerName.Contains("beverage") || lowerName.Contains("drink") || lowerName.Contains("पेय"))
                return "🥤";
            if (lowerName.Contains("bread") || lowerName.Contains("रोटी"))
                return "🍞";
            if (lowerName.Contains("sweet") || lowerName.Contains("मिठाई"))
                return "🍬";
            
            // Default icon
            return "🏪";
        }
        return "🛒";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
