﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using aehyok.Model.Authorization;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aehyok.Core.Data.Entity.Configurations.Authorization
{
    internal class RoleConfiguration : EntityMappingConfiguration<Role, int>
    {
        public override void Map(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(item => item.Id);
        }
    }
}
