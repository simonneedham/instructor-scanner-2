namespace InstructorScanner2.FunctionApp
{
    public class TestHtmlReport
    {
        private readonly ICalendarDaysPersistanceService _calendarDaysPersistanceService;
        private readonly IHtmlPageCreatorService _htmlPageCreatorService;

        public TestHtmlReport(
            ICalendarDaysPersistanceService calendarDaysPersistanceService,
            IHtmlPageCreatorService htmlPageCreatorService
        )
        {
            _calendarDaysPersistanceService = calendarDaysPersistanceService;
            _htmlPageCreatorService = htmlPageCreatorService;
        }

        //[FunctionName(nameof(TestHtmlReport))]
        //public async Task Run([TimerTrigger("0 57 00 23 12 *", RunOnStartup = true)]TimerInfo myTimer, ILogger logger)
        //{
        //    var calendarDays = await _calendarDaysPersistanceService.RetrieveAsync();
        //    await _htmlPageCreatorService.CreateHtmlPageAsync(calendarDays);
        //}
    }
}
