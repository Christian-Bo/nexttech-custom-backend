using System.Security.Cryptography;

namespace NextTech.Application.Modules.Orders;

public static class OrderCodeGenerator
{
    public static string Nuevo()
    {
        return $"ORD-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
    }
}
