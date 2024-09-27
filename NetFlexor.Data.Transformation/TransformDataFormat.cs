/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      This has the transformation functions for the data format.
 *      It is a syntax that is used to transform the data from the buffer to the output format.
 *      There are also function to use Jsonata to transform the data.
 * 
 */

using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NetFlexor.Interfaces;

namespace NetFlexor.Data.Transformation
{
    /// <summary>
    /// DataFormatParameters start with "$[" and end with "])". The parameters are separated by "|". The parameters are key value pairs separated by "=". <br></br>
    /// They are always at the end of the format string. I.E:<br></br>
    /// For loop -> $for(for loop content$[surround='"'|upper|sep="_|_"])<br></br>
    /// Tags, values or timestamps <br></br>
    ///          -> $(tag$[surround="'"'"|upper|sep=','])
    /// </summary>
    internal class DataFormatParameters
    {
        /// <summary>
        /// Surround the values with a character or a string. Can be presented as a string or a character within the configuration file. I.e. "," or ','
        /// </summary>
        public string surround { get; set; } = "";
        /// <summary>
        /// Convert the values to upper case
        /// </summary>
        public bool isUpper { get; set; } = false;
        /// <summary>
        /// Convert the values to lower case
        /// </summary>
        public bool isLower { get; set; } = false;
        /// <summary>
        /// Separator for the values. Can be presented as a string or a character within the configuration file. I.e. "," or ','
        /// </summary>
        public string sep { get; set; } = ",";

        /// <summary>
        /// The sentence that contains all the parameters. I.e. "tag$[surround="'"'"|upper|sep=',']"
        /// </summary>
        public string propertySentence { get; set; } = "";

        public string regexPattern
        {
            get
            {
                return propertySentence;
            }
        }

        /// <summary>
        /// Format how the timestamp should be serialized in the end result.
        /// </summary>
        public string timeStampFormat { get; set; } = "";

        public string getTimeStamp(DateTime time)
        {
            if (timeStampFormat.Contains("unix"))
            {
                string precision = timeStampFormat.Split('-')[1];
                switch (precision)
                {
                    case "ms":
                        return ((DateTimeOffset)time).ToUnixTimeMilliseconds().ToString();
                    case "us":
                        return (((DateTimeOffset)time).ToUnixTimeMilliseconds() * 1000).ToString();
                    case "ns":
                        return (((DateTimeOffset)time).ToUnixTimeMilliseconds() * 1000000).ToString();
                    case "ps":
                        return (((DateTimeOffset)time).ToUnixTimeMilliseconds() * 1000000000).ToString();
                    case "fs":
                        return (((DateTimeOffset)time).ToUnixTimeMilliseconds() * 1000000000000).ToString();
                    case "s":
                    default:
                        return ((DateTimeOffset)time).ToUnixTimeSeconds().ToString();
                }
            }
            else
                return String.Format(timeStampFormat, time);
        }

    }
    public static class TransformDataFormat
    {
        private static string _paramStart = "$[";
        private static string _paramEnd = "])";
        private static string _for = "$for(";
        private static string _parameter = "$(";
        private static string _ender = ")";

        /// <summary>
        /// Does not work since I have not figured out how to connect the format configuration to the input string.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="StringToParseDataFrom"></param>
        /// <returns></returns>
        private static List<FlexorDataBufferContainer> ReverseDataFormatParser(string format, string StringToParseDataFrom)
        {
            // Do the reverse that DataFormatParser does
            // format is the same as the input format in DataFormatParser
            // StringToParseDataFrom is the string that we want to parse the data from according to the format
            List<FlexorDataBufferContainer> containers = new();
            //$(tags$[surround:= '\"' || sep:=, ])
            //$for({\r\n            \"timestamp\" : $(timestamp$[format:=\"unix-s\"]),\r\n            \"data\" : [$(values$[sep:=, ])]\r\n        }$for[sep:=\",\\n\\t\\t\"])
            //format = "{\r\n    \"tags\" : [$(tags$[surround:= '\"' || sep:=, ])],\r\n    \"dataPoints\" : [\r\n        $for({\r\n            \"timestamp\" : $(timestamp$[format:=\"unix-s\"]),\r\n            \"data\" : [$(values$[sep:=, ])]\r\n        }$for[sep:=\",\\n\\t\\t\"])\r\n    ]\r\n}";
            format = "{\r\n    \"tags\": [$(tags$[surround:='\"'||sep:=\", \"])],\r\n    \"dataPoints\": [\r\n        $for({\r\n            \"timestamp\": $(timestamp$[format:=\"unix-s\"]),\r\n            \"data\": [$(values$[sep:=,])]\r\n        }$for[sep=\",\\n\\t\\t\"]\r\n    ]\r\n}";

            //StringToParseDataFrom = "{\r\n    \"tags\" : [\"apiTag_1\", \"apiTag_2\"],\r\n    \"dataPoints\" : [\r\n        {\r\n            \"timestamp\" : 1720006334,\r\n            \"data\" : [32.34,534.323]\r\n        },\r\n        {\r\n            \"timestamp\" : 1720006335,\r\n            \"data\" : [785.54,5342.21]\r\n        }\r\n    ]\r\n}";
            StringToParseDataFrom = "{\r\n    \"tags\": [\"apiTag_1\", \"apiTag_2\"],\r\n    \"dataPoints\": [\r\n        {\r\n            \"timestamp\": \"1720006334\",\r\n            \"data\": [32.34,534.323]\r\n        },\r\n        {\r\n            \"timestamp\": \"1720006335\",\r\n            \"data\": [785.54,5342.21]\r\n        }\r\n    ]\r\n}";

            //var dif1 = FindDifferences(format, StringToParseDataFrom);
            //var dif2 = FindDifferences(StringToParseDataFrom, format);
            var dif2 = FindDifferences(StringToParseDataFrom, format);
            
            var forLoopInds = GetDataInd(format, "$for(", ")", true);
            var otherInds = GetDataInd(format, "$(", ")", true);

            while (true)
            {
                // Break out if there are no more for loops or other placeholders to Read data from
                if ((otherInds.paramStart == -1 || otherInds.paramEnd == -1) && 
                    (forLoopInds.paramStart == -1 || forLoopInds.paramEnd == -1))
                    break;

                forLoopInds = GetDataInd(format, "$for(", ")", true);
                otherInds = GetDataInd(format, "$(", ")", true);

                if (otherInds.paramStart < forLoopInds.paramStart && otherInds.paramStart != -1)
                {
                    // Parse the place holder paremeters first
                    // otherInds paramStart index is the same as in the StringToParseDataFrom so now we know where to start reading the data from
                    var temp = format.Substring(otherInds.paramStart, otherInds.paramEnd - otherInds.paramStart);
                    // temp = $(tags$[surround='\"'|sep=,])

                    // find place holder DataFormatParameters
                    var data = parseDataFormatParameters(temp);

                    // find the placeholder name
                    var placeholder = temp.Substring(2, temp.Length - 3);
                    if (data.propertySentence.Length > 0)
                        placeholder = placeholder.Replace(data.propertySentence, "");
                    var tags = new List<string>();
                    var values = new List<string>();
                    var timestamps = new List<string>();
                    switch (placeholder)
                    {
                        case "tag":
                        case "tags":
                            // Read the tags from the StringToParseDataFrom from the starting index based on the DataFormatParameters parameters
                            string builder = "";
                            
                            break;
                        case "values":
                            // replace the placeholder with the value
                            //replace += $"{item.Value.ToString()}";
                            //format = format.Replace(temp, $"{data.surround}{container.DataElementArray[ind].Value.ToString()}{data.surround}");
                            break;
                        case "timestamps":
                            // replace the placeholder with the timestamp
                            //replace += $"{data.getTimeStamp(item.Timestamp)}";
                            //format = format.Replace(temp, $"{data.surround}{container.DataElementArray[ind].Timestamp.ToString()}{data.surround}");
                            break;
                    }
                }
            }

            //containers = DeParseForLoop(containers, format, StringToParseDataFrom);
            //containers = DeParseSingleContainer(containers, format, StringToParseDataFrom);

            return containers;
        }

        private static List<string> FindDifferences(string str1, string str2)
        {
            var wordsInStr1 = str1.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var wordsInStr2 = new HashSet<string>(str2.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            var differences = new List<string>();
            var currentDifference = new List<string>();

            foreach (var word in wordsInStr1)
            {
                if (!wordsInStr2.Contains(word))
                {
                    // If the word is not in str2, add it to the current difference
                    currentDifference.Add(word);
                }
                else if (currentDifference.Any())
                {
                    // If encountering a word that is in str2 and there are accumulated differences,
                    // join them and add to the differences list, then reset the current difference
                    differences.Add(string.Join(" ", currentDifference));
                    currentDifference.Clear();
                }
            }

            // Add any remaining differences after the loop
            if (currentDifference.Any())
            {
                differences.Add(string.Join(" ", currentDifference));
            }

            return differences;
        }

        public static string GeneralFormatParser(string format, FlexorDataBufferContainer container)
        {
            if (format is not null)
            {
                if (format.StartsWith("jsonata:"))
                {
                    // use jsonata to transform the data
                    return TransformJsonataFormat.TransformJson(JsonSerializer.Serialize(container),
                        format.Substring(8, format.Length - 8));
                }
                else
                    return DataFormatParser(format, container);
            }
            
            // Just give the containers "as is" if no format is defined
            return JsonSerializer.Serialize(container, new JsonSerializerOptions() { WriteIndented = true });
        }

        public static string DataFormatParser(string format, FlexorDataBufferContainer container)
        {
            if (format is null)
                return JsonSerializer.Serialize(container, new JsonSerializerOptions() { WriteIndented = true });
            else if (format.StartsWith("jsonata:"))
                return GeneralFormatParser(format, container);

            // example input format: $for(tagnames: $(tag), values: $(value), timestamps: $(timestamp))
            // search for $for( and the end of the for loop )
            (int start, int start_end) = TransformDataFormat.GetDataInd(format, "$start(", ")", false);
            string temp = "";
            if (start >= 0 && start_end >= 0)
            {
                var startContent = format.Substring(start, start_end - start);
                format = format.Replace(startContent, "");
                temp = startContent.Substring(7, startContent.Length - 8);
                temp = PopulateDataFormatPlaceholders(temp, container);
            }

            format = PopulateForLoop(format, container);
            // Populate other data in the format string
            format = PopulateDataFormatPlaceholders(format, container);

            return temp + format;
        }
        /*
         * Regex patterns:
         *  ^ - Starts with
         *  $ - Ends with
         *  [] - Range of characters
         *  () - Grouping
         *  . - Single character
         *  + - One or more characters once
         *  ? - optional preceding character match
         *  \ - Escape character
         *  \n - New line
         *  \d - Digit
         *  \D - Non-digit
         *  \s - Whitespace
         *  \S - Non-whitespace
         *  \w - Word character
         *  \W - Non-word character
         *  {x,y} - Repeat low (x) to high (y) times
         *  (x|y) - Either x or y
         *  [^x] - Any character except x
         */
        private static List<FlexorDataBufferContainer> DeParseForLoop(List<FlexorDataBufferContainer> containers, string format, string StringToParseDataFrom)
        {
            while (true)
            {
                var forLoopInds = GetDataInd(format, "$for(", ")", true);
                //var forLoopInds = getForLoop(format);
                if (forLoopInds.paramStart == -1 || forLoopInds.paramEnd == -1)
                    break; // Break out since there are no more for loops to populate

                // Parse the DataFormatParameters for the for loop out of the format string and into the DataFormatParameters object
                var data = parseDataFormatParametersForloop(format);

                // Extract the for loop content
                var forLoopContent = format.Substring(forLoopInds.paramStart, forLoopInds.paramEnd - forLoopInds.paramStart);
                //"([^"]*)"
                //'([^']*)'

                string pattern = "\"([^\"]*)\"";
                string patterns = "'([^']*)'";
            }

            return containers;
        }

        private static List<FlexorDataBufferContainer> DeParseSingleContainer(List<FlexorDataBufferContainer> containers, string format, string StringToParseDataFrom)
        {



            return containers;
        }

        private static string ExtractContentWithMatchingDelimiters(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 1)
            {
                return ""; // Input is either null, empty, or too short to contain delimiters and content
            }

            // Use the first character as the start delimiter
            if (input[0] == '"' || input[0] == '\'')
            {
                char delimiter = input[0];

                // Find the first occurrence of the same delimiter after the first character
                int endIndex = input.IndexOf(delimiter, 1);
                if (endIndex == -1)
                {
                    return ""; // Matching end delimiter not found
                }

                // Extract the content between the delimiters
                return input.Substring(1, endIndex - 1);
            }
            
            // There is no delimiter so return the input as is
            return input;
        }
        public static (int paramStart, int paramEnd) GetDataInd(string format, string start, string ender, bool isForLoop)
        {
            int paramStart = format.IndexOf(start);
            if (paramStart >= 0)
            {
                var count = 0;
                var startCharsFor = start.ToCharArray();
                if (isForLoop)
                {
                    //count = 1;
                    start = start.Replace("for", "");
                }
                else
                    count = 0;

                var startChars = start.ToCharArray();
                var endChars = ender.ToCharArray();

                int paramEnd = -1;
                

                for (int i = paramStart + start.Length; i < format.Length + 1; i++)
                {
                    //var temp = format[(i - 2)..i];
                    if (isForLoop && (IsFoundInIndex(format, i, startCharsFor) || IsFoundInIndex(format, i, startChars)) ||
                        !isForLoop && IsFoundInIndex(format, i, startChars))
                    {
                        count++;
                    }
                    else if (IsFoundInIndex(format, i, endChars))
                    {
                        count--;
                        if (count == 0)
                        {
                            paramEnd = i;
                            break;
                        }
                    }
                }
                return (paramStart, paramEnd);
            }
            return (-1, -1);
        }

        private static bool IsFoundInIndex(string format, int i, char[] chars)
        {
            //bool[] found = new bool[chars.Length];
            ////chars = chars.Reverse().ToArray();
            //for (int j = 0; j < chars.Length; j++)
            //{
            //    var temp = format[(i - 2)..i];
            //    if (format[i - j] == chars[j])
            //    {
            //        found[j] = true;
            //    }
            //}
            //bool result = found.Contains(false);
            //return !found.Contains(false);
            // Adjust the starting index for comparison based on the length of chars
            int start = i - chars.Length;

            // Ensure the start index is within the bounds of the format string
            if (start < 0) return false;

            //var temp = format[(i - chars.Length)..i];
            for (int j = 0; j < chars.Length; j++)
            {
                // Compare each character in chars with the corresponding character in the format string
                if (start + j > format.Length - 1 || format[start + j] != chars[j])
                {
                    return false; // If any character does not match, return false
                }
            }
            return true; // All characters matched, return true
        }
        private static List<string> SplitIgnoringQuotedPipe(string input)
        {
            var result = new List<string>();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            var temp = input.Split("||");
            //int start = 0;
            //for (int i = 0; i < input.Length; i++)
            //{
            //    switch (input[i])
            //    {
            //        case '\'':
            //            // Toggle the inSingleQuote flag if not within double quotes
            //            if (!inDoubleQuote) inSingleQuote = !inSingleQuote;
            //            break;
            //        case '\"':
            //            // Toggle the inDoubleQuote flag if not within single quotes
            //            if (!inSingleQuote) inDoubleQuote = !inDoubleQuote;
            //            break;
            //        case '|':
            //            // Split if not within any quotes
            //            if (!inSingleQuote && !inDoubleQuote)
            //            {
            //                result.Add(input.Substring(start, i - start));
            //                start = i + 1;
            //            }
            //            break;
            //    }
            //}
            //// Add the last segment if there's any
            //if (start < input.Length)
            //{
            //    result.Add(input.Substring(start));
            //}

            return result;
        }
        /// <summary>
        /// Parse the DataFormatParameters from the format string.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        private static DataFormatParameters parseDataFormatParameters(string format)
        {
            var paramInds = GetDataInd(format, "$[", _paramEnd, false);
            if (paramInds.paramStart >= 0 && paramInds.paramEnd >= 0)
            {
                var paramStr = format.Substring(paramInds.paramStart + 2, paramInds.paramEnd - paramInds.paramStart - 4);
                var param = new DataFormatParameters();
                param.propertySentence = $"$[{paramStr}]";
                //var paramStrs = SplitIgnoringQuotedPipe(paramStr);
                var paramStrs = paramStr.Split("||");
                //var paramStrs = paramStr.Split('|');
                foreach (var p in paramStrs)
                {
                    var keyVal = p.Split(":=");
                    switch (keyVal[0])
                    {
                        case "surround":
                            param.surround = ConvertEscapedSequences(ExtractContentWithMatchingDelimiters(keyVal[1]));
                            break;
                        case "upper":
                            param.isUpper = true;
                            break;
                        case "lower":
                            param.isLower = true;
                            break;
                        case "sep":
                            param.sep = ConvertEscapedSequences(ExtractContentWithMatchingDelimiters(keyVal[1]));
                            break;
                        case "format": // only used with timestamp
                            param.timeStampFormat = ConvertEscapedSequences(ExtractContentWithMatchingDelimiters(keyVal[1]));
                            break;
                        default:
                            break;
                    }
                }
                return param;
            }
            return new DataFormatParameters();
        }
        private static string ConvertEscapedSequences(string input)
        {
            // Manually replace known escape sequences
            return input
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                // Replace any other escaped sequence that starts with \\ followed by any character
                // This must be done last to correctly handle all specific sequences first
                .Replace("\\", @"\");
        }
        private static DataFormatParameters parseDataFormatParametersForloop(string format)
        {
            //var paramInds = getDataFormatParametersInd(format);
            var paramInds = GetDataInd(format, "$for[", _paramEnd, false);
            if (paramInds.paramStart >= 0 && paramInds.paramEnd >= 0)
            {
                var paramStr = format.Substring(paramInds.paramStart + 5, paramInds.paramEnd - paramInds.paramStart - 7);
                var param = new DataFormatParameters();
                param.propertySentence = $"$for[{paramStr}]";
                var paramStrs = SplitIgnoringQuotedPipe(paramStr);
                //var paramStrs = paramStr.Split('|');
                foreach (var p in paramStrs)
                {
                    var keyVal = p.Split('=');
                    switch (keyVal[0])
                    {
                        case "surround":
                            param.surround = ConvertEscapedSequences(ExtractContentWithMatchingDelimiters(keyVal[1]));
                            break;
                        case "upper":
                            param.isUpper = true;
                            break;
                        case "lower":
                            param.isLower = true;
                            break;
                        case "sep":
                            param.sep = ConvertEscapedSequences(ExtractContentWithMatchingDelimiters(keyVal[1]));
                            break;
                        default:
                            break;
                    }
                }
                return param;
            }
            return new DataFormatParameters();
        }
        

        private static string PopulateForLoop(string format, FlexorDataBufferContainer container)
        {
            while (true)
            {
                var forLoopInds = GetDataInd(format, "$for(", ")", true);
                //var forLoopInds = getForLoop(format);
                if (forLoopInds.paramStart == -1 || forLoopInds.paramEnd == -1)
                    break; // Break out since there are no more for loops to populate

                // Parse the DataFormatParameters for the for loop out of the format string and into the DataFormatParameters object
                var data = parseDataFormatParametersForloop(format);

                // Extract the for loop content
                var forLoopContent = format.Substring(forLoopInds.paramStart, forLoopInds.paramEnd - forLoopInds.paramStart);

                // Do the for loop
                var forLoopContentPopulated = "";
                //foreach (var item in container.DataElementArray)
                foreach (var dataContainer in container.DataContainer)
                {
                    // dataContainer ->
                    // TimeStamp
                    // List of DataContainerElements
                    foreach (var item in dataContainer.Data)
                    {
                        //var temp2 = forLoopContent.Replace(data.propertySentence, "");
                        var temp = format.Substring(forLoopInds.paramStart + 5, forLoopInds.paramEnd - forLoopInds.paramStart - 6);
                        //temp = temp.Replace("$for(", "");
                        // replace the placeholders in the for loop content i.e:
                        // $(tag) -> item.Tag
                        // $(value) -> item.Value
                        // $(timestamp) -> item.Timestamp
                        // NOTE: replace only the "tag", "value" and "timestamp" placeholders, not the placeholders in the DataFormatParameters
                        temp = temp.Replace("$(tags", $"$({item.Name}.tag");
                        temp = temp.Replace("$(values", $"$({item.Name}.value");
                        temp = temp.Replace("$(timestamps", $"$({item.Name}.timestamp");
                        temp = temp.Replace(data.propertySentence, "");
                        // populate the placeholders in the for loop content
                        temp = $"{data.surround}{PopulateDataFormatPlaceholders(temp, container)}{data.surround}";

                        if (container.DataContainer[container.DataContainer.Count - 1] != dataContainer)
                            temp += data.sep;

                        forLoopContentPopulated += temp;
                    }
                    ////var temp2 = forLoopContent.Replace(data.propertySentence, "");
                    //var temp = format.Substring(forLoopInds.paramStart + 5, forLoopInds.paramEnd - forLoopInds.paramStart - 6);
                    ////temp = temp.Replace("$for(", "");
                    ///
                    //// replace the placeholders in the for loop content i.e:
                    //// $(tag) -> item.Tag
                    //// $(value) -> item.Value
                    //// $(timestamp) -> item.Timestamp
                    //// NOTE: replace only the "tag", "value" and "timestamp" placeholders, not the placeholders in the DataFormatParameters
                    //temp = temp.Replace("$(tags", $"$({item.Tag}.tag");
                    //temp = temp.Replace("$(values", $"$({item.Tag}.value");
                    //temp = temp.Replace("$(timestamps", $"$({item.Tag}.timestamp");
                    //temp = temp.Replace(data.propertySentence, "");
                    //// populate the placeholders in the for loop content
                    //temp = $"{data.surround}{PopulateDataFormatPlaceholders(temp, container)}{data.surround}";

                    //if (container.DataElementArray[container.DataElementArray.Length - 1] != item)
                    //    temp += data.sep;

                    //forLoopContentPopulated += temp;

                    // continue by debugging this tomorrow
                }
                // replace the for loop with the populated for loop content
                format = format.Replace(forLoopContent, forLoopContentPopulated);
            }
            return format;
        }

        private static string PopulateDataFormatPlaceholders(string format, FlexorDataBufferContainer container)
        {
            // read the first placeholder
            (int start, int end) = GetDataInd(format, "$(", ")", false);
            while (true)
            {
                if (start == -1 || end == -1)
                    break; // Break out since there are no more placeholders to populate
                // find the first placeholder
                var temp = format.Substring(start, end - start);
                // find place holder DataFormatParameters
                var data = parseDataFormatParameters(temp);
                // find the placeholder name
                var placeholder = temp.Substring(2, temp.Length - 3);
                // remove the parameter sentence from the placeholder
                if (data.propertySentence.Length > 0)
                    placeholder = placeholder.Replace(data.propertySentence, "");
                // Switch case the placeholder name
                var pairs = placeholder.Split('.');
                if (pairs.Count() > 1)
                {
                    foreach (var item in container.DataContainer)
                    {
                        //int ind = Array.FindIndex(item.DataElementArray, x => x.Tag == pairs[0]);
                        int ind = item.Data.FindIndex(x => x.Name == pairs[0]);
                        switch (pairs[1])
                        {
                            case "tag":
                            case "name":
                                // replace the placeholder with the tag
                                format = format.Replace(temp, $"{data.surround}{item.Data[ind].Name}{data.surround}");
                                break;
                            case "value":
                                // replace the placeholder with the value
                                format = format.Replace(temp, $"{data.surround}{item.Data[ind].Value.ToString()}{data.surround}");
                                break;
                            case "timestamp":
                                // replace the placeholder with the timestamp
                                format = format.Replace(temp, $"{data.surround}{data.getTimeStamp(item.DateTimeStamp)}{data.surround}");
                                break;
                        }
                    }
                    
                }
                else
                {
                    string replace = "";
                    foreach (var dataContainer in container.DataContainer)
                    {
                        foreach (var item in dataContainer.Data)
                        {
                            if (placeholder == "timestamp")
                            {
                                replace += $"{data.getTimeStamp(dataContainer.DateTimeStamp)}";
                                break;
                            }
                            replace += $"{data.surround}";
                            switch (placeholder)
                            {
                                case "tags":
                                    // replace the placeholder with the tag
                                    replace += $"{item.Name}";
                                    //format = format.Replace(temp, $"{data.surround}{container.DataElementArray[ind].Tag}{data.surround}");
                                    break;
                                case "values":
                                    // replace the placeholder with the value
                                    replace += $"{item.Value.ToString()}";
                                    //format = format.Replace(temp, $"{data.surround}{container.DataElementArray[ind].Value.ToString()}{data.surround}");
                                    break;
                                case "timestamps":
                                    // replace the placeholder with the timestamp
                                    replace += $"{data.getTimeStamp(dataContainer.DateTimeStamp)}";
                                    //format = format.Replace(temp, $"{data.surround}{container.DataElementArray[ind].Timestamp.ToString()}{data.surround}");
                                    break;
                            }
                            replace += $"{data.surround}";

                            if (dataContainer != container.DataContainer.Last())
                                replace += data.sep;
                        }
                    }

                    format = format.Replace(temp, replace);
                }
                
                // read the next placeholder
                try
                {
                    (start, end) = GetDataInd(format, "$(", ")", false);
                }
                catch (Exception ex)
                {
                }
            }

            return format;
        }


        private static string PopulateAllDataFormatPlaceholders(string format, FlexorDataBufferContainer container)
        {
            // loop through the format string untill we have replaced all the placeholders
            (int start, int end) = GetDataInd(format, "$(", ")", false);
            while (start >= 0 && end >= 0)
            {
                // find the first placeholder
                var temp = format.Substring(start, end - start + 1);
                // find place holder DataFormatParameters
                var data = parseDataFormatParameters(temp);
                // find the placeholder name
                var placeholder = temp.Substring(2, temp.Length - 3);
                // Switch case the placeholder name
                

                //(start, end) = getDataInd(format, "$(", ")");
            }
            return format;
        }

    }
}