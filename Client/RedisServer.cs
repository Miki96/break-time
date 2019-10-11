using System.Threading.Tasks;
using StackExchange.Redis;

namespace Client
{
    class RedisServer
    {
        public static async Task<ISubscriber> GetSubscriber(string server)
        {
            ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(server);
            IDatabase db = redis.GetDatabase();
            ISubscriber sub = redis.GetSubscriber();
            return sub;
        }
    }
}
