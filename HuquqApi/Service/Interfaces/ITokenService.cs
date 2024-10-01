
using HuquqApi.Model;
using HuquqApi.Settings;

namespace HuquqApi.Service.Interfaces
{
    public interface ITokenService
    {
        string GetToken(IList<string> userRole,User user, JwtSetting jwtSetting);
    }
}
