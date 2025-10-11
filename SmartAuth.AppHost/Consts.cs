namespace SmartAuth.AppHost;

public static class Consts
{
    public const string DatabaseName = "authdb";
    public const string DatabaseUsername = "postgres";
    public const string DatabasePassword = "postgres";
    public const string DatabasePort = "5432";
    public const string DatabaseHostname = "auth-db";

    public static int ToInt(this string str)
    {
        return Convert.ToInt32(str);
    }
}