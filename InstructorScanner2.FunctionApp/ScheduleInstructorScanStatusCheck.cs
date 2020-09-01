using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InstructorScanner2.FunctionApp
{
    public class ScheduleInstructorScanStatusCheck
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ISendEmailService _sendEmailService;
        private readonly ICalendarDaysPersistanceService _calendarDayPersistanceService;

        public ScheduleInstructorScanStatusCheck(
            IOptions<AppSettings> appSettings,
            ISendEmailService sendEmailService,
            ICalendarDaysPersistanceService calendarDayPersistanceService
        )
        {
            _appSettings = appSettings;
            _sendEmailService = sendEmailService;
            _calendarDayPersistanceService = calendarDayPersistanceService;
        }

        [FunctionName(nameof(ScheduleInstructorScanStatusCheck))]
        public async Task Run(
            [TimerTrigger("0 05 06 * * *")]TimerInfo myTimer,
            ILogger logger,
            CancellationToken cancellationToken
        )
        {
            var emailContent = new List<string>();
            var previousCalendarDays = await _calendarDayPersistanceService.RetrieveAsync(cancellationToken);

            if(previousCalendarDays == null)
            {
                emailContent.Add("Unable to retrieve previous calendar days.");
            }
            else
            {
                emailContent.Add("Currently tracking the following instructors/slots:");
                emailContent.Add(string.Empty);

                var distinctIntials = previousCalendarDays.GetDistinctIntials();

                foreach(var initial in distinctIntials)
                {
                    var freeSlotCount = previousCalendarDays.CalculateTotalSlotCountForInstructor(initial);
                    emailContent.Add($"    {initial}: {freeSlotCount} slots");
                }
            }

            emailContent.Add(string.Empty);
            emailContent.Add($"Slot summary: {_appSettings.Value.WebRootUrl}");

            await _sendEmailService.SendEmailAsync("Instructor Scan Status", emailContent, MimeType.Html, cancellationToken);
        }
    }
}
