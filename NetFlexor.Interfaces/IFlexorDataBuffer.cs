/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Interface for the data buffer service.
 * 
 */

namespace NetFlexor.Interfaces
{
    /// <summary>
    /// Buffer service interface
    /// </summary>
    public interface IFlexorDataBuffer
    {

        FlexorDataBufferObject GetFlexorBuffer(string serviceName, INetFlexorService service);
        List<string> GetFlexorBufferNames();
        long GetQueueSizeInBytes(string serviceName);
        void EnqueueFlexorBuffer(string serviceName, INetFlexorService service, FlexorDataBufferContainer container);
        void EnqueueToServicesFlexorBuffer(string serviceName, FlexorDataBufferContainer container);
        bool TryDequeueFlexorBufferContainer(string serviceName, out FlexorDataBufferContainer? container);
    }
}
