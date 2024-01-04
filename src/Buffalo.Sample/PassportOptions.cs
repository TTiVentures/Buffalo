namespace Buffalo.Sample
{
	public class PassportOptions
	{
		public bool RequireAuthentication { get; set; } = false;
		public string? Audience { get; set; }
		public string Authority { get; set; } = string.Empty;
        public string? ClientId { get; set; } = string.Empty;
        public string? ClientSecret { get; set; } = string.Empty;
    }
}
