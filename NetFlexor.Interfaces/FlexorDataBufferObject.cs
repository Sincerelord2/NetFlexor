/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      FlexorDataBufferObject, a buffer object for FlexorDataBufferContainer objects.
 * 
 */

namespace NetFlexor.Interfaces
{
    public class FlexorDataBufferObject
    {
        public long queueSizeInBytes { get; set; } = 0;
        public Queue<FlexorDataBufferContainer> bufferQueue { get; set; } = new();
    }
}
