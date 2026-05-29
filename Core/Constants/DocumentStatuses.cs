namespace Core.Constants
{
    /// <summary>
    /// Defines document lifecycle status values used by upload, processing, indexing, and RAG workflows.
    /// </summary>
    public static class DocumentStatuses
    {
        public const string Uploaded = "uploaded";
        public const string Processing = "processing";
        public const string Indexed = "indexed";
        public const string Failed = "failed";
    }
}
