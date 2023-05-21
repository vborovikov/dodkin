namespace Dodkin.Service.Data;

using System;
using System.Text;

static class Extensions
{
    public static string ToText(this TimeSpan timeSpan)
    {
        var text = new StringBuilder();

        if (timeSpan.Days > 0)
        {
            text.Append(timeSpan.Days).Append('d');
        }
        if (timeSpan.Hours > 0)
        {
            if (text.Length > 0)
                text.Append(' ');
            text.Append(timeSpan.Hours).Append('h');
        }
        if (timeSpan.Minutes > 0)
        {
            if (text.Length > 0)
                text.Append(' ');
            text.Append(timeSpan.Minutes).Append('m');
        }
        if (timeSpan.Seconds > 0)
        {
            if (text.Length > 0)
                text.Append(' ');
            text.Append(timeSpan.Seconds).Append('s');
        }
        if (timeSpan.Milliseconds > 0)
        {
            if (text.Length > 0)
                text.Append(' ');
            text.Append(timeSpan.Milliseconds).Append("ms");
        }

        return text.ToString();
    }
}
