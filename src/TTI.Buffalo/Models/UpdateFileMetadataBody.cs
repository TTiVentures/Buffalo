namespace TTI.Buffalo.Models;

public record UpdateFileMetadataBody
{
    public required AccessLevels ReadAccessLevel { get; set; }
    public SecurityClaims? ReadSecurityClaims { get; set; }

    public required AccessLevels WriteAccessLevel { get; set; }
    public SecurityClaims? WriteSecurityClaims { get; set; }
}