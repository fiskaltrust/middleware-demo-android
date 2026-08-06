namespace fiskaltrust.AndroidLauncher.AndroidService
{
    public static class PosSystemApiServiceContract
    {
        public const string LauncherPackage = "eu.fiskaltrust.androidlauncher";

        public const string ServiceClass = "eu.fiskaltrust.androidlauncher.PosSystemAPIService";

        public const string Permission = "eu.fiskaltrust.androidlauncher.permission.POSSYSTEMAPI";

        public const int MsgRequest = 1;

        public const int MsgReply = 2;

        public const string KeyCorrelationId = "CorrelationId";

        public const string KeyMethod = "Method";
        public const string KeyPath = "Path";
        public const string KeyHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
        public const string KeyBodyBase64Url = "BodyBase64Url";

        public const string KeyStatusCode = "StatusCode";
        public const string KeyContentBase64Url = "ContentBase64Url";
        public const string KeyContentTypeBase64Url = "ContentTypeBase64Url";
        public const string KeyResponseHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
    }
}
