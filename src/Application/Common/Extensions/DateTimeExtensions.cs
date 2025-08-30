namespace Application.Common.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime(),
            _ => dateTime.ToUniversalTime(),
        };
    }

    public static int GetAge(this DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}