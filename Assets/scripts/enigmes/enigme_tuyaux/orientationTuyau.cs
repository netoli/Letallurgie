public enum orientationTuyau
{
    Zero,
    QuatreVingtDix,
    CentQuatreVingt,
    DeuxCentSoixanteDix
}

public static class orientationTuyauExtensions
{
    public static float EnDegres(this orientationTuyau orientation)
    {
        switch (orientation)
        {
            case orientationTuyau.Zero: return 0f;
            case orientationTuyau.QuatreVingtDix: return 90f;
            case orientationTuyau.CentQuatreVingt: return 180f;
            case orientationTuyau.DeuxCentSoixanteDix: return 270f;
            default: return 0f;
        }
    }

    public static orientationTuyau Suivante(this orientationTuyau orientation)
    {
        return (orientationTuyau)(((int)orientation + 1) % 4);
    }
}