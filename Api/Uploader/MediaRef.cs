namespace Api.Uploader
{
    /// <summary>
    /// List of changes when replacing media refs 
    /// </summary>
    public class MediaRef
    {
        public string Type { get; set; }
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Field { get; set; }
        public string Url => $"/en-admin/{Type.ToLower()}/{Id.ToString()}";
    }
}
