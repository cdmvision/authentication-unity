namespace Cdm.Authentication.Browser
{
    public class BrowserResult
    {
        public BrowserStatus status { get; }
        public string response { get; }
        public string error { get; }

        public bool isSuccess => status == BrowserStatus.Success;
        
        public BrowserResult(BrowserStatus status, string response)
        {
            this.status = status;
            this.response = response;
        }
        
        public BrowserResult(BrowserStatus status, string response, string error)
        {
            this.status = status;
            this.response = response;
            this.error = error;
        }
    }
}