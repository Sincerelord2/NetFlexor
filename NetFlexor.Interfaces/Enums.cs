/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      public enums for the NetFlexor application
 * 
 */

namespace NetFlexor.Interfaces;
public enum timeUnit
{
    ms,
    s,
    m,
    h,
    d
}

public enum ExecutionFormat
{
    Parallel,
    Sequence
}

public enum HttpServiceType
{
    DataTransfer,
    DataRead
}