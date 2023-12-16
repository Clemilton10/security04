using IdentityModel.Client;
using System.Threading.Tasks;

public interface ITokenService
{
	Task<TokenResponse> GetToken(string scope);
}
