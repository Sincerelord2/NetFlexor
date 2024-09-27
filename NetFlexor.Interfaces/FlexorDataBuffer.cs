/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Buffer for FlexorDataBufferContainer objects.
 *      It is used to store FlexorDataBufferContainer objects for services.
 *      
 */

using NetFlexor.ServiceHelpers;

namespace NetFlexor.Interfaces
{
    /// <summary>
    /// Buffer for FlexorDataBufferContainer objects. <br></br>
    /// You can queue and dequeue objects or get the whole buffer.
    /// </summary>
    public class FlexorDataBuffer : IFlexorDataBuffer
    {
        private Dictionary<(string serviceName, INetFlexorService service), FlexorDataBufferObject> _buffers = new Dictionary<(string, INetFlexorService), FlexorDataBufferObject>();
        
        private object _bufferLock = new object();

        /// <summary>
        /// Returns the buffer object for the given service. <br></br>
        /// If it does not exist, it will create a new one.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public FlexorDataBufferObject GetFlexorBuffer(string serviceName, INetFlexorService service)
        {
            if (_buffers.TryGetValue((serviceName, service), out var buffer))
            {
                return buffer;
            }
            var newBuffer = new FlexorDataBufferObject();
            _buffers.Add((serviceName, service), newBuffer);
            return newBuffer;
        }
        /// <summary>
        /// Returns the whole buffer.
        /// </summary>
        /// <returns></returns>
        public Dictionary<(string serviceName, INetFlexorService service), FlexorDataBufferObject> GetWholeBuffer()
        {
            // don't think there is need for this?
            // maybe only for buffer handler service?
            return _buffers;
        }
        /// <summary>
        /// Returns the size of the queue in bytes for the given service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public long GetQueueSizeInBytes(string serviceName)
        {
            return _buffers.Where(x => x.Key.serviceName == serviceName).Sum(x => x.Value.queueSizeInBytes);
        }
        /// <summary>
        /// Returns the names of the buffers.
        /// </summary>
        /// <returns></returns>
        public List<string> GetFlexorBufferNames()
        {
            return _buffers.Select(x => x.Key.serviceName).ToList();
        }
        /// <summary>
        /// Enqueues a FlexorDataBufferContainer object to the buffer of the given service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="service"></param>
        /// <param name="container"></param>
        public void EnqueueFlexorBuffer(string serviceName, INetFlexorService service, FlexorDataBufferContainer container)
        {
            var buf = GetFlexorBuffer(serviceName, service);
            long size = ObjectSizeCalculator.GetObjectSize(container);
            buf.queueSizeInBytes += size; // remember to add size the queue size
            buf.bufferQueue.Enqueue(container);
        }
        /// <summary>
        /// Enqueues a FlexorDataBufferContainer object to the buffer of the given service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="container"></param>
        public void EnqueueToServicesFlexorBuffer(string serviceName, FlexorDataBufferContainer container)
        {
            foreach (var b in _buffers)
            {
                if (b.Key.serviceName == serviceName)
                {
                    b.Value.bufferQueue.Enqueue(container); 
                    b.Value.queueSizeInBytes += ObjectSizeCalculator.GetObjectSize(container); // remember to add size the queue size
                }
            }
        }
        /// <summary>
        /// Tries to dequeue a FlexorDataBufferContainer object from the buffer of the given service. <br></br>
        /// Returns true if successful.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public bool TryDequeueFlexorBufferContainer(string serviceName, out FlexorDataBufferContainer? container)
        {
            foreach (var b in _buffers)
            {
                if (b.Key.serviceName == serviceName && b.Value.bufferQueue.TryDequeue(out var buf))
                {
                    container = buf;
                    long size = ObjectSizeCalculator.GetObjectSize(container);
                    b.Value.queueSizeInBytes -= size;
                    return true;
                }
            }
            container = null;
            return false;
        }
    }
}
