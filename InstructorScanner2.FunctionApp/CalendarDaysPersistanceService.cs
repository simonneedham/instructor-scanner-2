using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InstructorScanner2.FunctionApp
{
    public interface ICalendarDaysPersistanceService
    {
        Task<List<CalendarDay>> RetrieveAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task StoreAsync(List<CalendarDay> calendarDays, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class CalendarDaysPersistanceService : ICalendarDaysPersistanceService
    {
        private const string CONTAINER_NAME = "calendarDays";
        private const string FLYING_CLUB = "FlyingClub";

        private readonly IOptions<AppSettings> _appSettingOptions;
        private static  Lazy<JsonSerializer> _jsonSerializer = new Lazy<JsonSerializer>(() =>
            {
                var js = new JsonSerializer();
                js.ContractResolver = new CamelCasePropertyNamesContractResolver();
                return js;
            });

        public CalendarDaysPersistanceService(
            IOptions<AppSettings> appSettingOptions
        )
        {
            _appSettingOptions = appSettingOptions;
        }

        public async Task<List<CalendarDay>> RetrieveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var calendarDays = new List<CalendarDay>();

            using (var cosmosClient = new CosmosClient(_appSettingOptions.Value.CosmosDbAccountEndPoint, _appSettingOptions.Value.CosmosDbAccountKey))
            {
                var container = await GetContainerAsync(cosmosClient, cancellationToken);
                var resultSet = container
                    .GetItemQueryIterator<CalendarDay>(queryDefinition: null);

                while (resultSet.HasMoreResults)
                {
                    var calDaysResponse = await resultSet.ReadNextAsync(cancellationToken);
                    calendarDays.AddRange(calDaysResponse);
                }
            }

            return calendarDays;
        }

        public async Task StoreAsync(List<CalendarDay> calendarDays, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var cosmosClient = new CosmosClient(_appSettingOptions.Value.CosmosDbAccountEndPoint, _appSettingOptions.Value.CosmosDbAccountKey))
            {
                var container = await GetContainerAsync(cosmosClient, cancellationToken);

                foreach (var calDay in calendarDays)
                {
                    using(var stream = ToStream(calDay))
                    using (var responseMessage = await container.UpsertItemStreamAsync(stream, new PartitionKey(FLYING_CLUB), null, cancellationToken))
                    {
                        // Item stream operations do not throw exceptions for better performance
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            throw new InstructorScanException($"Failed to store slots for calendar day {calDay.Date:dd-MMM-yyyy}. Status code: {responseMessage.StatusCode} Message: {responseMessage.ErrorMessage}");
                        }
                    }
                }
            }
        }

        private async Task<Container> GetContainerAsync(CosmosClient cosmosClient, CancellationToken cancellationToken)
        {
            var dbName = _appSettingOptions.Value.CosmosDbDatabaseName;
            var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(id: dbName, cancellationToken: cancellationToken);
            if (dbResponse.StatusCode != HttpStatusCode.OK) throw new InstructorScanException($"Failed to create Cosmos db '{dbName}'");

            Container container;
            try
            {
                container = cosmosClient.GetContainer(dbName, CONTAINER_NAME);
            }
            catch(Exception)
            {
                var containerResponse = await cosmosClient
                    .GetDatabase(dbName)
                    .DefineContainer(CONTAINER_NAME, "/flyingClub")
                    .WithDefaultTimeToLive(-1)
                    .WithUniqueKey().Path("/date")
                    .Attach()
                    .CreateIfNotExistsAsync();
                if (containerResponse.StatusCode != HttpStatusCode.OK) throw new InstructorScanException($"Failed to create container '{CONTAINER_NAME}'");

                container = containerResponse.Container;
            }

            return container;
        }

        private static Stream ToStream<T>(T input)
        {
            var streamPayload = new MemoryStream();
            using (var streamWriter = new StreamWriter(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
            using (var writer = new JsonTextWriter(streamWriter))
            {                
                writer.Formatting = Formatting.None;
                _jsonSerializer.Value.Serialize(writer, input);
                writer.Flush();
                streamWriter.Flush();
            }

            streamPayload.Position = 0;
            return streamPayload;
        }

        private static T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)stream;
                }

                using (var sr = new StreamReader(stream))
                using (var jsonTextReader = new JsonTextReader(sr))
                {

                    return _jsonSerializer.Value.Deserialize<T>(jsonTextReader);
                }
            }
        }
    }
}
