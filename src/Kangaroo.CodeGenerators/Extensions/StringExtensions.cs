// Licensed to Kangaroo under one or more agreements.
// We license this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.CodeGenerators.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal static class StringExtensions
    {
        public static string FirstCharToLowerCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            {
                return str;
            }

            return char.ToLower(str[0]) + str.Substring(1);
        }
    }
}
