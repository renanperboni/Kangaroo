// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Exceptions
{
    using System;

    public class KangarooSecurityException : KangarooException
    {
        public KangarooSecurityException(KangarooErrorCode internalErrorCode = KangarooErrorCode.SecurityValidation, string additionalInfo = null)
            : base(internalErrorCode, null, additionalInfo)
        {
        }
    }
}
