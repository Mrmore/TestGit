﻿using System;
using System.Globalization;
#if SILVERLIGHT
using System.Windows.Media;
#else
using Windows.UI;
#endif

namespace Microsoft.VideoAdvertising
{
    internal static class ConversionHelper
    {
        public static object Change(object o, Type t)
        {
            if (o == null) return null;
            string s = o.ToString();
            
            if (t == typeof(bool))
            {
                return ParseBool(s);
            }

            return Convert.ChangeType(o, t, null);
        }

        public static bool ParseBool(string s)
        {
            return (s == "1" || s.ToUpper() == "TRUE");
        }

        /// <summary>
        /// </summary>
        /// <param name="base">The base uri to use if the string is not absolute</param>
        /// <param name="uriString">The uri string, relative or absolute</param>
        /// <returns>Resulting merged uri object</returns>
        public static Uri GetAbsoluteUri(Uri @base, String uriString)
        {
            if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                string f = Uri.EscapeUriString(uriString);
                if (Uri.IsWellFormedUriString(f, UriKind.Absolute))
                {
                    return new Uri(f);
                }
                return new Uri(@base, uriString);
            }
            else
            {
                return new Uri(uriString);
            }
        }

        /// <summary>
        /// Converts a string of an ARGB color to a System.color type
        /// </summary>
        /// <param name="color">The ARGB color profile to parse</param>
        /// <returns>The resulting System.color type</returns>
        public static Color ColorFromString(String color)
        {
            UInt32 uiValue = UInt32.Parse(color.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier | System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            byte a = (byte)((uiValue >> 0x18) & 0xFF);
            byte r = (byte)((uiValue >> 0x10) & 0xFF);
            byte g = (byte)((uiValue >> 0x08) & 0xFF);
            byte b = (byte)((uiValue) & 0xFF);
            return Color.FromArgb(a, r, g, b);
        }
    }
}
