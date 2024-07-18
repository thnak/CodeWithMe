namespace BusinessModels.Resources;

public static class PageRoutes
{
    public static class Error
    {
        public const string Name = "/Error";
        public const string ErrorPage = Name + "/error-page";
        public const string Default403 = "/403";
        public const string NotFound = Name + "/404";
        public const string Default404 = "/404";
        public const string UnAuthorized = Name + "/403";
    }

    public static class Home
    {
        public const string Root = "/home";
    }

    public static class Drive
    {
        public const string Name = "/drive";
        public const string Index = Name + "/page";
        public const string Shared = Name + "/shared";
    }

    public static class Account
    {
        public const string Name = "/Account";
        public const string Profile = Name + "/profile";
        public const string SignIn = Name + "/login";
        public const string SignInError = SignIn + "/error";

        public const string SignUp = Name + "register";
        public const string SignUpError = SignUp + "/error";

        public const string Logout = Name + "/logout";
        public const string Denied = Name + "/403";
    }
}