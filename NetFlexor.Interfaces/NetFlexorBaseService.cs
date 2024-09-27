/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Base service for the all the services in the application.
 *      Works as a interface for the services to interact with the buffer.
 * 
 */
namespace NetFlexor.Interfaces
{
    /// <summary>
    /// Base service class for all. It provides the buffer and the methods to interact with the buffer.
    /// </summary>
    public abstract class NetFlexorBaseService : INetFlexorService
    {
        /// <summary>
        /// This is the same buffer that is used by all services
        /// </summary>
        private IFlexorDataBuffer _buffer;

        /// <summary>
        /// Constructor for the base service. Must be called by the derived classes.
        /// </summary>
        /// <param name="buffer"></param>
        public NetFlexorBaseService(IFlexorDataBuffer buffer)
        {
            _buffer = buffer;
        }
        /// <summary>
        /// Base method for the services to work.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public virtual Task WorkAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// Initializes the service with the configuration.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public virtual Task InitializeAsync(IFlexorServiceConfiguration conf, CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
        /// <summary>
        /// Enqueues the data containers to the buffer.
        /// </summary>
        /// <param name="container"></param>
        protected void EnqueueDataToBuffer(FlexorDataBufferContainer container)
        {
            _buffer.EnqueueFlexorBuffer(container.ServiceName, this, container);
        }
        /// <summary>
        /// Enqueues the data containers to the buffer of the specified service
        /// </summary>
        /// <param name="container"></param>
        /// <param name="serviceName"></param>
        protected void EnqueueDataToServiceBuffer(FlexorDataBufferContainer container, string serviceName)
        {
            // used by the input services, like http listener and buffer handler
            _buffer.EnqueueToServicesFlexorBuffer(container.ServiceName, container);
        }
        /// <summary>
        /// Reads the buffer size of the specified service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        protected long ReadServiceBufferSize(string serviceName)
        {
            return _buffer.GetQueueSizeInBytes(serviceName);
        }
        /// <summary>
        /// Tries to dequeue the data from the buffer. Returns true if successful.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="syncData"></param>
        /// <returns></returns>
        protected bool TryDequeueDataFromBuffer(string serviceName, out FlexorDataBufferContainer? syncData)
        {
            if (_buffer.TryDequeueFlexorBufferContainer(serviceName, out var data))
            {
                syncData = data;
                return true;
            }
            syncData = null;
            return false;
        }
        /// <summary>
        /// Returns the buffer object.
        /// </summary>
        /// <returns></returns>
        public IFlexorDataBuffer GetFlexorBuffer()
        {
            return _buffer; // required for the buffer handler service for easier buffer handling
        }
    }
}