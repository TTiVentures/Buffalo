namespace Buffalo.Sample
{
	public class PassportOptions
	{
		public bool RequireAuthentication { get; set; } = false;
		public string? RequiredClaim { get; set; }
		public string Authority { get; set; } = string.Empty;
	}
}
