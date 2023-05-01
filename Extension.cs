using System;
using Microsoft.EntityFrameworkCore;

namespace EFCore
{
    public static class Extension
    {
        public static IQueryable<GuildDto> GuildToDto(this IQueryable<Guild> guild)
        {
            return guild.Select(g => new GuildDto()
            {
                GuildId = g.GuildId,
                Name = g.GuildName,
                MemberCount = g.Members.Count 
            });
        }
    }
}

