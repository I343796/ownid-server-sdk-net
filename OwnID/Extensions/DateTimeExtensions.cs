// ReSharper disable once CheckNamespace
namespace System
{
    public static class DateTimeExtensions
    {
        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            return new((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public static DateTime RoundToMinute(this DateTime dt)
        {
            return RoundUp(dt, TimeSpan.FromMinutes(1));
        }
    }
}