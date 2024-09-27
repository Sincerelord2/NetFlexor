/*
 * 
 * Author: Sincerelord2
 * 
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Data buffer container for the application.
 *      It is structured to also support the gRPC and http interfaces.
 * 
 */

/* Buffer container json format if serialized:
{
  "Name": "httpListener_1",         # Optional, will be overwriten by the service
  "DataContainer": [		        # Data container with that specific timestamp
	{
	  "TimeFormat": "unix-s",
	  "TimeStamp": "1630512000",
	  "Data": [
        {
            "Name": "data_1",       # Name of the data
            "Value": 2.1            # Data (type of object in C#)
        },
        {
            "Name": "data_2",
            "Data": 54.2
        },
        ...
		{
			"Name": "data_N",
			"Data": "Last data point"
		}
      ]
	}
  ]
}
 */

using System.Globalization;
using System.Text.Json.Serialization;

namespace NetFlexor.Interfaces
{
    public interface IFlexorDataBufferContainer
    {
        string ServiceName { get; set; }
        List<FlexorDataBufferDataContainer> DataContainer { get; set; }
        //Dictionary<ContainerDataTimeHandler, Dictionary<string, object>> DataContainer { get; set; }
    }
    

    public class FlexorDataBufferContainer : IFlexorDataBufferContainer
    {
        public string ServiceName { get; set; }
        public List<FlexorDataBufferDataContainer> DataContainer { get; set; } = new();
        //public Dictionary<ContainerDataTimeHandler, Dictionary<string, object>> DataContainer { get; set; } = new();
        public FlexorDataBufferContainer()
        {
            
        }
        public FlexorDataBufferContainer(string serviceName)// : this()
        {
            ServiceName = serviceName;
        }

        public void AddArrayDataToContainer(List<string> tags, List<object> data, DateTime Date)
        {
            if (DataContainer.Count == 0)
                DataContainer.Add(new FlexorDataBufferDataContainer(tags, data, Date));
            else if (DataContainer.Any(x => x.DateTimeStamp == Date))
            {
                var sameDate = DataContainer.Where(x => x.DateTimeStamp == Date);
                // check if the tags are not the same in this list
                var existingTags = sameDate.SelectMany(x => x.Data.Select(d => d.Name));
                var duplicateTags = tags.Intersect(existingTags);

                if (duplicateTags.Any())
                    throw new Exception("Duplicate tags found with same timestamp.");
                else
                {
                    foreach (var item in sameDate)
                    {
                        for (int i = 0; i < tags.Count; i++)
                        {
                            item.Data.Add(new FlexorDataBufferDataContainerElement()
                            {
                                Name = tags[i],
                                Value = data[i]
                            });
                        }
                    }
                }
            }
        }
    }

    public class FlexorDataBufferDataContainer
    {
        public string TimeFormat { get; set; } = "DateTime";
        public object TimeStamp
        {
            get
            {
                return NetFlexorTimeConverter.ConvertTimeToObjectFormat(_dateTimeStamp, TimeFormat);
            }
            set
            {
                _dateTimeStamp = NetFlexorTimeConverter.ConvertToDateTime(value.ToString(), TimeFormat) ?? throw new Exception("Invalid time format.");
            }
        }

        [JsonIgnore]
        private DateTime _dateTimeStamp;

        [JsonIgnore]
        public DateTime DateTimeStamp
        {
            get
            {
                return _dateTimeStamp;
            }
            set
            {
                _dateTimeStamp = value;
            }
        }

        public List<FlexorDataBufferDataContainerElement> Data { get; set; } = new();
        public FlexorDataBufferDataContainer()
        {
            
        }
        public FlexorDataBufferDataContainer(List<string> tags, List<object> data, DateTime Date)
        {
            DateTimeStamp = Date;
            for (int i = 0; i < tags.Count; i++)
            {
                Data.Add(new FlexorDataBufferDataContainerElement()
                {
                    Name = tags[i],
                    Value = data[i]
                });
            }
        }
    }

    public class FlexorDataBufferDataContainerElement
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
