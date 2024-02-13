namespace TTI.Buffalo.Models;

public record UpdateFileMetadataBody
{
    public required AccessLevels ReadAccessLevel { get; set; }
    public required SecurityClaims? ReadSecurityClaims { get; set; }

    public required AccessLevels WriteAccessLevel { get; set; }
    public required SecurityClaims? WriteSecurityClaims { get; set; }
}