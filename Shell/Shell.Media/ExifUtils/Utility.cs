#region License
/*---------------------------------------------------------------------------------*\

	Distributed under the terms of an MIT-style license:

	The MIT License

	Copyright (c) 2005-2009 Stephen M. McKamey

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.

\*---------------------------------------------------------------------------------*/
#endregion License

using System;
using System.ComponentModel;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace ExifUtils
{
    /// <summary>
    /// Internal Utility Class
    /// </summary>
    internal static class Utility
    {
        #region Constants

        private const string FlagsDelim = ", ";

        #endregion Constants

        #region Enumeration Methods

        /// <summary>
        /// Checks if an enum is able to be combined as bit flags.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsFlagsEnum (Type type)
        {
            if (!type.IsEnum) {
                return false;
            }

            Attribute flags = Utility.GetAttribute (type, typeof(FlagsAttribute), true);
            return (flags != null);
        }

        /// <summary>
        /// Splits a bitwise-OR'd set of enums into a list.
        /// </summary>
        /// <param name="enumType">the enum type</param>
        /// <param name="value">the combined value</param>
        /// <returns>list of flag enums</returns>
        public static Enum[] GetFlagList (Type enumType, object value)
        {
            ulong longVal = Convert.ToUInt64 (value);
            string[] enumNames = Enum.GetNames (enumType);
            Array enumValues = Enum.GetValues (enumType);

            List<Enum> enums = new List<Enum> (enumValues.Length);

            // check for empty
            if (longVal == 0L) {
                // Return the value of empty, or zero if none exists
                if (Convert.ToUInt64 (enumValues.GetValue (0)) == 0L) {
                    enums.Add (enumValues.GetValue (0) as Enum);
                } else {
                    enums.Add (null);
                }
                return enums.ToArray ();
            }

            for (int i = enumValues.Length - 1; i >= 0; i--) {
                ulong enumValue = Convert.ToUInt64 (enumValues.GetValue (i));

                if ((i == 0) && (enumValue == 0L)) {
                    continue;
                }

                // matches a value in enumeration
                if ((longVal & enumValue) == enumValue) {
                    // remove from val
                    longVal -= enumValue;

                    // add enum to list
                    enums.Add (enumValues.GetValue (i) as Enum);
                }
            }

            if (longVal != 0x0L) {
                enums.Add (Enum.ToObject (enumType, longVal) as Enum);
            }

            return enums.ToArray ();
        }

        #endregion Enumeration Methods

        #region Attribute Methods

        /// <summary>
        /// Gets an attribute of an object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="attributeType"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static Attribute GetAttribute (object value, Type attributeType, bool inherit)
        {
            if (value == null || attributeType == null) {
                return null;
            }

            object[] array;

            if (value is Type) {
                array = (value as Type).GetCustomAttributes (attributeType, inherit);
            } else if (value is MemberInfo) {
                array = (value as MemberInfo).GetCustomAttributes (attributeType, inherit);
            } else if (value is Enum) {
                array = value.GetType ().GetField (value.ToString ()).GetCustomAttributes (attributeType, inherit);
            } else {
                throw new NotSupportedException ("object doesn't support attributes.");
            }

            return (array != null && array.Length > 0) ? array [0] as Attribute : null;
        }

        /// <summary>
        /// Gets the value of the DescriptionAttribute for the object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription (object value)
        {
            if (value == null) {
                return null;
            }

            DescriptionAttribute attribute =
                Utility.GetAttribute (value, typeof(DescriptionAttribute), false) as DescriptionAttribute;
            return (attribute != null) ? attribute.Description : null;
        }

        /// <summary>
        /// Gets the value of the DescriptionAttribute for the enum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescription (Enum value)
        {
            if (!Utility.IsFlagsEnum (value.GetType ())) {
                return Utility.GetDescription ((object)value);
            }

            Enum[] enumValues = Utility.GetFlagList (value.GetType (), value);
            StringBuilder builder = new StringBuilder ();

            for (int i = 0; i < enumValues.Length; i++) {
                builder.Append (Utility.GetDescription (enumValues [i] as object));

                if (i != enumValues.Length - 1) {
                    builder.Append (FlagsDelim);
                }
            }

            return builder.ToString ();
        }

        #endregion Attribute Methods
    }
}
