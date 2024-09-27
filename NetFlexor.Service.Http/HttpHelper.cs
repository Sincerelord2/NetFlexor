/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Http helper functions.
 * 
 */

using System.Net;

namespace NetFlexor.Service.Http
{
    internal static class HttpHelper
    {
        /// <summary>
        /// Function to check if the incoming HTTP method is allowed
        /// </summary>
        /// <param name="allowedHttpMethods"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsHttpMethodAllowed(HttpMethod[] allowedHttpMethods, HttpListenerRequest request)
        {
            if (allowedHttpMethods.Length > 0)
            {
                bool allowed = false;
                foreach (var item in allowedHttpMethods)
                {
                    if (item.ToString() == request.HttpMethod)
                    {
                        allowed = true;
                        break;
                    }
                }

                // do the corresponding action if the method is not allowed
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if the incoming IP-Address is allowed
        /// </summary>
        /// <param name="allowedSources"></param>
        /// <param name="enpointAddress"></param>
        /// <returns></returns>
        public static bool IsSourceIpAddressAllowed(string[] allowedSources, string enpointAddress)
        {
            // Check if the input IP-Address is allowed
            if (allowedSources != null && allowedSources.Length > 0 && allowedSources[0] != "*.*.*.*/0")
            {
                // Allowed source format: 192.168.10.20/24
                // Or: 192.168.10.20/32
                // We need to check if the source IP-Address is in the allowed range
                //string enpointAddress = request.RemoteEndPoint.Address.ToString();

                // Check if the subnet is allowed
                bool allowed = false;
                foreach (var item in allowedSources)
                {
                    if (item.Contains("/"))
                    {
                        // Check if the source IP-Address is in the allowed range
                        string[] parts = item.Split('/');
                        string[] sourceParts = enpointAddress.Split('.');
                        string[] allowedParts = parts[0].Split('.');
                        int mask = int.Parse(parts[1]);
                        bool match = true;
                        for (int i = 0; i < mask / 8; i++)
                        {
                            if (sourceParts[i] != allowedParts[i])
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            allowed = true;
                            break;
                        }
                    }
                    else
                    {
                        if (item == enpointAddress)
                        {
                            allowed = true;
                            break;
                        }
                    }
                }

                // do the corresponding action if the source is not allowed
                if (!allowed)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
