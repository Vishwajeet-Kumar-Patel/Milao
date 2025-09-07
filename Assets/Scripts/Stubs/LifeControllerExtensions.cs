// Adds "fake" overloads so calls compile even without the real Firebase-backed method.
public static class LifeControllerExtensions
{
    public static void SaveLivesDataBase(this LifeController _self) {}
    public static void SaveLivesDataBase(this LifeController _self, int lives) {}
    public static void SaveLivesDataBase(this LifeController _self, int lives, params object[] extra) {}
}
