using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InstructorScanner2.FunctionApp
{
    public class ScheduledInstructorScan
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ICalendarDaysPersistanceService _calendarDaysPersistanceService;
        private readonly IHtmlPageCreatorService _htmlPageCreatorService;
        private readonly ISendEmailService _sendEmailService;

        public ScheduledInstructorScan(
            IOptions<AppSettings> appSettings,
            ICalendarDaysPersistanceService calendarDaysPersistanceService,
            IHtmlPageCreatorService htmlPageCreatorService,
            ISendEmailService sendEmailService
        )
        {
            _appSettings = appSettings;
            _calendarDaysPersistanceService = calendarDaysPersistanceService;
            _htmlPageCreatorService = htmlPageCreatorService;
            _sendEmailService = sendEmailService;
        }

        [FunctionName("ScheduledInstructorScanTimerTrigger")]
        public async Task TimerTrigger(
            [TimerTrigger("0 5 11,19 * * *", RunOnStartup = true)] TimerInfo myTimer,
            ILogger logger,
            CancellationToken cancellationToken
            )
        {
            var instructorCount = _appSettings.Value.Instructors.Count;
            var started = DateTime.UtcNow;

            logger.LogInformation($"Initiating scan of {instructorCount} instructors for {_appSettings.Value.DaysToScan} days at {started: dd-MMM-yyy HH:mm:ss}");

            // get the previous calendar days
            var previousCalendarDays = await _calendarDaysPersistanceService.RetrieveAsync(cancellationToken);

            // build a list of the days to scan
            var allDatesToScan = Enumerable
                .Range(1, _appSettings.Value.DaysToScan)
                .Select(offset => DateTime.Today.AddDays(offset))
                .ToList();

            // scan all the dates
            var newCalendarDays = await ScanInstructorBookings(logger, allDatesToScan);

            // determine the calendar changes
            var calendarChanges = CalendarDaysComparer.Compare(previousCalendarDays, newCalendarDays);

            // save the new calendar scan results
            await _calendarDaysPersistanceService.StoreAsync(newCalendarDays, cancellationToken);
            await _htmlPageCreatorService.CreateHtmlPageAsync(newCalendarDays, cancellationToken);


            // send an email if necessary
            logger.LogInformation($"{calendarChanges.Count} calendar changes found.");
            if (calendarChanges.Count > (instructorCount * 2))
            {
                logger.LogInformation("New changes found, sending an email.");

                calendarChanges.Add(string.Empty);
                calendarChanges.Add($"Slot summary: {_appSettings.Value.WebRootUrl}");

                await _sendEmailService.SendEmailAsync("FI Booking Scan Results", calendarChanges, MimeType.Html, cancellationToken);
            }
            else
            {
                logger.LogInformation($"Not sending an email as {calendarChanges.Count} calendar changes line count is less than or equal to the minimum of {instructorCount * 2}");
            }

            // finish
            var stopped = DateTime.UtcNow;
            var runTime = stopped - started;
            logger.LogInformation($"Scan completed after {runTime.Minutes}m {runTime.Seconds}s.");
        }

        private async Task<List<CalendarDay>> ScanInstructorBookings(ILogger logger, List<DateTime> allDatesToScan)
        {
            var daysPerScan = _appSettings.Value.DaysPerScan;
            var newCalendarDays = new List<CalendarDay>();
            using (var bpp = new BookingPageParser(_appSettings, logger))
            {
                foreach (var activityDatesChunk in Chunk(allDatesToScan, daysPerScan))
                {
                    var startDate = activityDatesChunk.First();
                    var endDate = activityDatesChunk.Last();

                    logger.LogInformation($"Parsing bookings between {startDate:dd/MM/yyyy} and {endDate:dd/MM/yyyy}");

                    try
                    {
                        var calendarDays = await bpp.GetBookings(startDate, endDate, _appSettings.Value.Instructors);
                        newCalendarDays.AddRange(calendarDays);
                        Task.Delay(_appSettings.Value.DelaySecondsPerScan * 1000).Wait();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to parse bookings between between {startDate:dd/MM/yyyy} and {endDate:dd/MM/yyyy}.");
                    }
                }
            }

            return newCalendarDays;
        }

        private static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
        {
            var pos = 0;
            while (source.Skip(pos).Any())
            {
                yield return source.Skip(pos).Take(chunkSize);
                pos += chunkSize;
            }
        }
    }
}