using System.Collections.Generic;

namespace InstructorScanner2.FunctionApp
{
    public class AppSettings
    {
        public string RootUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string LoginPage { get; set; }
        public string LoginPostEndpoint { get; set; }
        public string InstructorsEndpoint { get; set; }
        public string BookingsEndpoint { get; set; }
        public List<Instructor> Instructors { get; set; }
        public int DaysToScan { get; set; }
        public string StorageConnectionString { get; set; }
        public string SendGridApiKey { get; set; }
        public string ToEmailAddress { get; set; }
        public string FromEmailAddress { get; set; }
        public string WebRootUrl { get; set; }
        public string CosmosDbDatabaseName { get; set; }
        public string CosmosDbAccountKey { get; set; }
        public string CosmosDbAccountEndPoint { get; set; }
        public int DelaySecondsPerScan { get; set; }
        public int DaysPerScan { get; set; }
    }
}
