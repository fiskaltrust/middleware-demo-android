namespace fiskaltrust.AndroidLauncher.AndroidService
{
    /// <summary>
    /// Wire contract for the launcher PosSystemAPIService. Kept in a separate,
    /// dependency-free type so client apps can copy or share these constants
    /// without pulling in the whole launcher assembly.
    /// </summary>
    public static class PosSystemApiServiceContract
    {
        // ---- Binding ----------------------------------------------------------------

        /// <summary>Package name of the middleware launcher.</summary>
        public const string LauncherPackage = "eu.fiskaltrust.androidlauncher";

        /// <summary>Fully-qualified service class name to use with <c>ComponentName</c>.</summary>
        public const string ServiceClass = "eu.fiskaltrust.androidlauncher.PosSystemAPIService";

        /// <summary>Signature-level permission enforced on the bound service.</summary>
        public const string Permission = "eu.fiskaltrust.androidlauncher.permission.POSSYSTEMAPI";

        // ---- Message.What values ----------------------------------------------------

        /// <summary>Client -> service. Payload described by the request Key* fields.</summary>
        public const int MsgRequest = 1;

        /// <summary>Service -> client (via <c>Message.ReplyTo</c>). Payload described by the reply Key* fields.</summary>
        public const int MsgReply = 2;

        // ---- Bundle keys (shared: request + reply carry CorrelationId) --------------

        /// <summary>Opaque string set by the client, echoed in the reply for correlation.</summary>
        public const string KeyCorrelationId = "CorrelationId";

        // ---- Request bundle keys (same names as the previous Intent extras) --------

        public const string KeyMethod = "Method";
        public const string KeyPath = "Path";
        public const string KeyHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
        public const string KeyBodyBase64Url = "BodyBase64Url";

        // ---- Reply bundle keys ------------------------------------------------------

        public const string KeyStatusCode = "StatusCode";
        public const string KeyContentBase64Url = "ContentBase64Url";
        public const string KeyContentTypeBase64Url = "ContentTypeBase64Url";
        // Note: intentionally reuses the old response-header key name for parity with
        // the previous intent-based contract.
        public const string KeyResponseHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
    }
}
