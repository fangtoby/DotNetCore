﻿using aehyok.Core.Data.Entity.Configurations.Blog;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace aehyok.Core.Data.Entity
{
    /// <summary>
    /// EntityFramework-CodeFirst数据上下文
    /// </summary>
    public class CodeFirstDbContext : IdentityDbContext<IdentityUser>
    {
        public CodeFirstDbContext(DbContextOptions options) :
            base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.AddConfiguration(new ArticleConfiguration());

            builder.AddConfiguration(new TagConfiguration());

            builder.AddConfiguration(new ArticleTagConfiguration());

            builder.AddConfiguration(new CommentConfiguration());

        }
    }
}
