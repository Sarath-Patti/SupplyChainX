namespace SupplyChainX.Application.Common.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = "SupplyChainX_Super_Secret_Jwt_Signing_Key_2026_Enterprise_Secure!";
    public string Issuer { get; set; } = "SupplyChainX";
    public string Audience { get; set; } = "SupplyChainXClients";
    public int ExpiryMinutes { get; set; } = 120;
}
