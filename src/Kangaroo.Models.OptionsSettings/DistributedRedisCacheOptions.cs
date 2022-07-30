// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.OptionsSettings
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DistributedRedisCacheOptions
    {
        public const string DistributedRedisCache = "DistributedRedisCache";

        public string ConnectionString { get; set; } = string.Empty;

        public string InstanceName { get; set; } = string.Empty;
    }
}
