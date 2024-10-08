using Geniapp.Frontend.Models;
using Geniapp.Infrastructure.Database.MasterDatabase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Geniapp.Frontend.Controllers;

[Route("/shards")]
[ApiController]
public class ShardsController(MasterDbContext masterDbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<ShardDto>> GetShards()
    {
        Shard[] shards = await masterDbContext.Shards.ToArrayAsync();
        List<ShardDto> result = [];

        foreach (Shard shard in shards)
        {
            result.Add(new ShardDto { ShardId = shard.Id, Name = shard.Name });
        }

        return result;
    }
}
