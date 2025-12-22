namespace GQuiz.Extensions
{
    public static class SessionExtensions
    {
        public static bool IsLoggedIn(this ISession session)
        {
            return session.GetInt32("UserId") != null;
        }

        public static bool IsHost(this ISession session)
        {
            return session.GetString("IsHost") == "true";
        }
    }
}