using Api.Payments;
using Api.Startup;

namespace Api.Uploader;


[ListAs("ProductImages")]
[ImplicitFor("ProductImages", typeof(Product))]

public partial class Upload {}