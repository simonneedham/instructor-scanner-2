using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstructorScanner2.FunctionApp
{
    public class BookingPageParser : IDisposable
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;
        private CookieContainer _cookies;
        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;
        private readonly Uri _rootUrl;
        private Dictionary<string, string> _instructorDictionary;

        public BookingPageParser(IOptions<AppSettings> appSettingsOptions, ILogger logger)
        {
            _appSettings = appSettingsOptions.Value;
            _logger = logger;
            _rootUrl = new Uri(appSettingsOptions.Value.RootUrl);
            _instructorDictionary = new Dictionary<string, string>();
        }

        public async Task<IList<CalendarDay>> GetBookings(DateTime startDate, DateTime endDate, IList<Instructor> instructors)
        {
            if (_httpClient == null)
                await InitHttpClientAsync();

            // Get Instructors
            await UpdateInstructorsAsync(startDate, endDate);

            // Get Bookings
            var bookingsUrl = _appSettings
                .BookingsEndpoint
                .Replace("[start-date]", $"{startDate:ddd MMM dd yyyy} 00:00:00 GMT+0100 (British Summer Time)")
                .Replace("[end-date]", $"{endDate:ddd MMM dd yyyy} 00:00:00 GMT+0100 (British Summer Time)");

            var bookingsResponse = await _httpClient.PostAsync(new Uri(_rootUrl, bookingsUrl), null);
            bookingsResponse.EnsureSuccessStatusCode();
            var bookingsJson = await JsonSerializer.DeserializeAsync<IList<InstructorEventJson>>(await bookingsResponse.Content.ReadAsStreamAsync(), new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });
            var bookingsWithInstructors = bookingsJson.Where(bj => !string.IsNullOrEmpty(bj.resourceId)).ToList();


            //var bookingPageUrl = new Uri(_rootUrl, $"{_appSettings.BookingPage}?dt={date.ToString("dd/MM/yyyy")}");
            //var bookingPageResponse = await _httpClient.GetAsync(bookingPageUrl);
            //bookingPageResponse.EnsureSuccessStatusCode();

            //var bookingPageContents = await bookingPageResponse.Content.ReadAsStringAsync();

            //var bookingPageParser = new HtmlDocument();
            //bookingPageParser.LoadHtml(bookingPageContents);

            //var tableBookings = bookingPageParser.DocumentNode.SelectSingleNode("//table[@id='tblBookings']");
            //if (tableBookings == null) throw new InstructorScanException("Could not find table with an id of 'tblBookings'");

            //var times = new List<string>();
            //var timesTDNodes = tableBookings.SelectNodes(".//td[@class='TimeHeaderHalf']");
            //foreach(var td in timesTDNodes)
            //{
            //    var time = td.GetDirectInnerText();
            //    times.Add(time);
            //    times.Add($"{time}:30");
            //}

            ////- Find row where tr / td innerText = 'Instructor Name'
            //var instructorSlotsList = new List<InstructorSlots>();
            //foreach (var instctr in instructors)
            //{
            //    var instructorSlots = new InstructorSlots
            //    {
            //        InstructorInitials = instctr.Initials,
            //        Slots = new List<Slot>()
            //    };

            //    var instructorRowNode = tableBookings.SelectSingleNode($".//tr[td='{instctr.Name}']");
            //    if (instructorRowNode != null)
            //    {

            //        _logger.LogInformation("Found instructor row");

            //        var originalBookingTds = instructorRowNode.SelectNodes(".//td[not(@class='HeaderCellAc')]");
            //        var bookings = new List<string>();
            //        foreach (var tdNode in originalBookingTds)
            //        {
            //            var status = BookingStatus(tdNode);
            //            var statusCount = GetColSpanValue(tdNode);

            //            for (var i = 0; i < statusCount; i++)
            //            {
            //                bookings.Add(status);
            //            }
            //        }

            //        if (times.Count != bookings.Count)
            //            throw new InstructorScanException("Eeek!! Time slot count doesn't match bookings count!");


            //        for (var i = 0; i < bookings.Count; i++)
            //        {
            //            instructorSlots.Slots.Add(new Slot { Availability = bookings[i], Time = times[i] });
            //        }
            //    }

            //    instructorSlotsList.Add(instructorSlots);
            //}

            //var calendarDay = new CalendarDay{ InstructorSlots = instructorSlotsList };
            //calendarDay.SetDate(date);

            return new List<CalendarDay>();
        }

        private async Task UpdateInstructorsAsync(DateTime startDate, DateTime endDate)
        {
            var date = startDate.Date;
            while(date <= endDate)
            {
                await UpdateInstructorsForDateAsync(date);
                date = date.AddDays(1);
                Task.Delay(_appSettings.DelaySecondsPerScan * 1000).Wait();
            }
        }

        private async Task UpdateInstructorsForDateAsync(DateTime bookingDate)
        {
            var instructorsResponse = await _httpClient.PostAsync(new Uri(_rootUrl, _appSettings.InstructorsEndpoint), new FormUrlEncodedContent(new Dictionary<string, string>() { { "start_date", $"{bookingDate:ddd MMM dd yyyy} 00:00:00 GMT+0100 (British Summer Time)" } }));
            instructorsResponse.EnsureSuccessStatusCode();
            var instructorsJson = await JsonSerializer.DeserializeAsync<IList<InstructorJson>>(await instructorsResponse.Content.ReadAsStreamAsync(), new JsonSerializerOptions { IgnoreNullValues = true, PropertyNameCaseInsensitive = true });

            foreach (var i in instructorsJson)
                _instructorDictionary[i.id] = i.title;
        }

        private async Task InitHttpClientAsync()
        {
            _cookies = new CookieContainer();
            _httpClientHandler = new HttpClientHandler { CookieContainer = _cookies };
            _httpClient = new HttpClient(_httpClientHandler);

            var loginPageUrl = new Uri(_rootUrl, _appSettings.LoginPage);
            var loginPageHtml = await _httpClient.GetStringAsync(loginPageUrl);
            var dictionaryLoginInputs = GetDictionaryLoginInputs(loginPageHtml);

            var loginPostUrl = new Uri(_rootUrl, _appSettings.LoginPostEndpoint);
            var loginPageResponse = await _httpClient.PostAsync(loginPostUrl, new FormUrlEncodedContent(dictionaryLoginInputs));
            loginPageResponse.EnsureSuccessStatusCode();
        }

        private Dictionary<string, string> GetDictionaryLoginInputs(string loginPageHtml)
        {
            var loginPageParser = new HtmlDocument();
            loginPageParser.LoadHtml(loginPageHtml);

            var inputs = loginPageParser.DocumentNode.SelectNodes("//form[@name='login']//input");

            var dictionary = new Dictionary<string, string>();
            foreach (var input in inputs)
            {
                dictionary.Add(input.GetAttributeValue("name", string.Empty), input.GetAttributeValue("value", string.Empty));
            }

            dictionary["txtEmailMM"] = _appSettings.Username;
            dictionary["txtPasswordMM"] = _appSettings.Password;

            return dictionary;
        }

        public void WriteCookiesToConsole(Uri uri)
        {
            if (_cookies == null) return;

            foreach (var cookie in _cookies.GetCookies(uri).Cast<Cookie>())
            {
                _logger.LogInformation($"{cookie.Name}: {cookie.Value}");
            }
        }


        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cookies = null;
                    if (_httpClientHandler != null) _httpClientHandler.Dispose();
                    if (_httpClient != null) _httpClient.Dispose();

                    _httpClientHandler = null;
                    _httpClient = null;

                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        private class InstructorJson
        {
            public string id { get; set; }
            public string title { get; set; }
            public string backgorund_colour { get; set; }
            public string colour { get; set; }
        }

        private class InstructorEventJson
        {
            public bool allDay { get; set; }
            public string end { get; set; } //2020-09-07 11:00:00
            public string id { get; set; }
            public string resourceId { get; set; }
            public string start { get; set; } //2020-09-07 09:00:00
            public string title { get; set; }
        }
    }
}
