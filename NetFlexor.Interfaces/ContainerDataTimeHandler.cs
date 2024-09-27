/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Time converter for NetFlexor
 * 
 */

using System.Globalization;

namespace NetFlexor.Interfaces
{
    public static class NetFlexorTimeConverter
    {
        public static DateTime? ConvertToDateTime(string input, string timeFormat)
        {
            // Check for Unix time in seconds
            switch (timeFormat)
            {
                case "unix-s":
                    if (long.TryParse(input, out long unixSeconds))
                    {
                        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).DateTime;
                    }
                    break;
                case "unix-ms":
                    if (long.TryParse(input, out long unixMilliseconds))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).DateTime;
                    }
                    break;
                case "unix-us":
                    if (long.TryParse(input, out long unixMicroseconds))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(unixMicroseconds / 1000).DateTime;
                    }
                    break;
                case "unix-ns":
                    if (long.TryParse(input, out long unixNanoseconds))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(unixNanoseconds / 1000000).DateTime;
                    }
                    break;
                case "unix-ps":
                    if (long.TryParse(input, out long unixPicoseconds))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(unixPicoseconds / 1000000000).DateTime;
                    }
                    break;
                default:
                    // Try to convert based on the ISO 8601
                    if (DateTime.TryParseExact(input, timeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                        return dateTime;
                    if (DateTime.TryParse(input, out dateTime))
                        return dateTime;
                    break;
            }
            // If none of the formats match, return null
            return null;
        }

        public static string ConvertTimeToStringFormat(DateTime dateTime, string timeFormat)
        {
            var temp = ConvertTimeToObjectFormat(dateTime, timeFormat).ToString();
            if (temp is null)
                throw new Exception("Invalid time format.");
            return temp;
        }

        public static object ConvertTimeToObjectFormat(DateTime dateTime, string timeFormat)
        {
            switch (timeFormat)
            {
                case "unix-s":
                    return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
                case "unix-ms":
                    return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
                case "unix-us":
                    return (new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() * 1000);
                case "unix-ns":
                    return (new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() * 1000000);
                case "unix-ps":
                    return (new DateTimeOffset(dateTime).ToUnixTimeMilliseconds() * 1000000000);
                case "datetime": // default datetime format
                    return dateTime.ToString("o");
                default: // ISO 8601
                    return dateTime.ToString(timeFormat, CultureInfo.InvariantCulture);
            }
        }
    }
    //public class ContainerDataTimeHandler
    //{
    //    public long UnixStamp { get; set; }
    //    public DateTime TimeStamp { get; set; }
    //    public ContainerDataTimeHandler(long unix, string unixFormat)
    //    {
    //        UnixStamp = unix;
    //    }
    //    public ContainerDataTimeHandler(DateTime time)
    //    {
    //        TimeStamp = time;
    //    }
    //}
}
