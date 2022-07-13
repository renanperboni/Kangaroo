// Licensed to Kangaroo under one or more agreements.
// We license this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Kangaroo.Models.Entitites
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IEntityHandlerRequest<TEntity> : IRequest
        where TEntity : class, IEntity
    {
        public TEntity Entity { get; set; }
    }
}
