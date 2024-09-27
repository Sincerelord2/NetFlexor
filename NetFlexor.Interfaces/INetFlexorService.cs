/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *       Base for all Application services.
 * 
 */

namespace NetFlexor.Interfaces
{
    public interface INetFlexorService
    {
        Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken);
        Task WorkAsync(CancellationToken stoppingToken);
    }
}
