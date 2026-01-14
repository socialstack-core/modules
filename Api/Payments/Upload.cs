using Api.Payments;
using Api.Startup;

namespace Api.Uploader;


[ListAs("ProductImages", IsPrimary = false)]
[ImplicitFor("ProductImages", typeof(Product))]

[ListAs("ProductDownloads", IsPrimary = false)]
[ImplicitFor("ProductDownloads", typeof(Product))]

public partial class Upload { }