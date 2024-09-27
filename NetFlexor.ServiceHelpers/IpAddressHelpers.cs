/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      IpAddressHelpers.cs is a helper class for checking if the incoming IP-Address is allowed.
 * 
 */

namespace NetFlexor.ServiceHelpers
{
    public static class IpAddressHelpers
    {
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
