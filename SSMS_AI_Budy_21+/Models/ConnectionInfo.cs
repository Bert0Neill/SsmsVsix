namespace SSMS_AI_Buddy.Models
{
    public class ConnectionInfo
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public bool IsConnected { get; set; }
        public string Caption { get; set; }

        public override string ToString()
        {
            return IsConnected ? $"{Server}.{Database}" : "[Not Connected]";
        }
    }
}
