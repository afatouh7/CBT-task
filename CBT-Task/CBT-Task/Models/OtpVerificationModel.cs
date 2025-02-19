namespace CBT_Task.Models
{
    public class OtpVerificationModel
    {
        public int UserId { get; set; }
        public string OtpCode { get; set; } = string.Empty;
    }
}
