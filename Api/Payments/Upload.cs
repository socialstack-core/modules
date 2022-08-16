using Api.Payments;
using Api.Startup;

namespace Api.Uploader;


[ListAs("ProductImages", false)]
[ImplicitFor("ProductImages", typeof(Product))]

public partial class Upload {}