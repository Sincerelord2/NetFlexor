/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This transforms the JSON data with JSONata expression by using the Jsonata.Net library.
 * 
 */

using Jsonata.Net.Native;

namespace NetFlexor.Data.Transformation
{
    public class TransformJsonataFormat
    {
        public static string TransformJson(string stringToTransform, string transformationString)
        {
            // stringToTransform is the JSON string containing the real data
            // transformationString is the JSONata expression for the transformation

            // Create a JsonataExpression instance with the provided JSONata expression (str2)
            var query = new JsonataQuery(transformationString);

            // Execute the expression against the input JSON (str1)
            var result = query.Eval(stringToTransform);

            // Convert the result to a JSON string and return it
            return result.ToString();
        }
    }
}
