// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Exceptions
{
    using System;

    public class KangarooException : Exception
    {
        public KangarooException(int? errorCode = null, string additionalInfo = null)
        {
            this.ErrorCode = errorCode;
            this.AdditionalInfo = additionalInfo;
        }

        public int? ErrorCode { get; }

        public string AdditionalInfo { get; }
    }
}
