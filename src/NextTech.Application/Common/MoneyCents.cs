namespace NextTech.Application.Common;

public static class MoneyCents
{
    public static int FromQuetzales(decimal monto)
    {
        var redondeado = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        if (redondeado <= 0)
        {
            throw new AppValidationException("El monto a cobrar debe ser mayor que cero.");
        }

        return (int)decimal.Round(redondeado * 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal ToQuetzales(int centavos)
    {
        if (centavos <= 0)
        {
            throw new AppValidationException("El monto a cobrar debe ser mayor que cero.");
        }

        return centavos / 100m;
    }
}
