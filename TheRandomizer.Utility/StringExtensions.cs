﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TheRandomizer.Utility
{
    public static class StringExtensions
    {
        /// <summary>
        /// Attempts to deserialize the provided <see cref="string"/> into the type specified by <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to deserialize the <see cref="string"/> into</typeparam>
        /// <param name="extended">The <see cref="string"/> to deserialize</param>
        /// <param name="value">The <typeparamref name="T"/> object resulting from the deserialization</param>
        /// <returns>True if the deserialization was successful, otherwise false.</returns>
        public static bool TryDeserialize<T>(this string extended, out T value) where T : class
        {
            try
            {
                value = extended.Deserialize<T>();
                return true;
            }
            catch
            {
                value = null;
            }
            return false;
        }

        /// <summary>
        /// Deserializes a <see cref="string"/> into the type specified by <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the <see cref="string"/> into</typeparam>
        /// <param name="extended">The <see cref="string"/> to deserialize</param>
        /// <returns>An object of type <typeparamref name="T"/></returns>
        public static T Deserialize<T>(this string extended) where T : class
        {
            using (var reader = new StringReader(extended))
            {
                var deserializer = new XmlSerializer(typeof(T));
                return deserializer.Deserialize(reader) as T;
            }
        }

        /// <summary>
        /// Returns true if the <paramref name="extended"/> is an <see cref="int"/>
        /// </summary>
        /// <param name="extended">The <see cref="string"/> to evaluate</param>
        /// <returns>True if the <paramref name="extended"/> is an <see cref="int"/>; else False</returns>
        public static bool IsInteger(this string extended)
        {
            int value;
            return int.TryParse(extended, out value);            
        }

        /// <summary>
        /// Returns true if the <paramref name="extended"/> is numeric
        /// </summary>
        /// <param name="extended">The <see cref="string"/> to evaluate</param>
        /// <returns>True if the <paramref name="extended"/> is numeric; else False</returns>
        public static bool IsNumeric(this string extended)
        {
            double value;
            return double.TryParse(extended, out value);
        }

        /// <summary>
        /// Removes all instances of the values in <paramref name="toRemove"/> from <paramref name="extended"/>
        /// </summary>
        /// <param name="extended">The source <see cref="string"/></param>
        /// <param name="toRemove">An array of <see cref="string"/> to remove from <paramref name="extended"/></param>
        /// <returns>The <paramref name="extended"/> string with the items in <paramref name="toRemove"/> removed</returns>
        public static string Remove(this string extended, params string[] toRemove)
        {
            var value = extended;
            foreach (var s in toRemove)
            {
                extended.Replace(s, string.Empty);
            }
            return value;
        }

        /// <summary>
        /// Splits a <see cref="string"/> into an <see cref="IEnumerable{string}"/> on new lines
        /// </summary>
        /// <param name="extended">The <see cref="string"/> to split</param>
        /// <param name="keepEmptyLines">If true, empty strings will be included in the result</param>
        /// <returns>An <see cref="IEnumerable{string}"/> of the lines in the <paramref name="extended"/> <see cref="string"/></returns>
        public static IEnumerable<string> SplitLines(this string extended, bool keepEmptyLines)
        {
            using (System.IO.StringReader reader = new System.IO.StringReader(extended))
            {
                var line = string.Empty;
                do
                {
                    line = reader.ReadLine();
                    if (keepEmptyLines || !string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                } while (line != null);
            }
        }

        /// <summary>
        /// Mimics the VB.NET Left method to return the a <see cref="string"/> consisting of the <paramref name="count"/> left most characters from <paramref name="extended"/>
        /// </summary>
        /// <param name="extended">The source <see cref="string"/></param>
        /// <param name="count">The number of characters to return</param>
        /// <returns>A <see cref="string"/> consisting of the <paramref name="count"/> left most characters from <paramref name="extended"/></returns>
        public static string Left(this string extended, Int32 count)
        {
            if (extended.Length < count) return extended;
            return extended.Substring(count);
        }

        /// <summary>
        /// Mimics the VB.NET Right method to return the a <see cref="string"/> consisting of the <paramref name="count"/> right most characters from <paramref name="extended"/>
        /// </summary>
        /// <param name="extended">The source <see cref="string"/></param>
        /// <param name="count">The number of characters to return</param>
        /// <returns>A <see cref="string"/> consisting of the <paramref name="count"/> right most characters from <paramref name="extended"/></returns>
        public static string Right(this string extended, Int32 count)
        {
            if (extended.Length < count) return extended;
            return extended.Substring(extended.Length - count);
        }
    }
}
