using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.IO;
using System.Threading.Tasks;

namespace Api.Ftp
{
	
	/// <summary>
	/// A helper service for handling SFTP servers.
	/// </summary>
	public partial class FtpService : AutoService
	{

		/// <summary>
		/// Starts an SFTP connection
		/// </summary>
		/// <param name="host"></param>
		/// <param name="user"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public async Task<SftpClient> StartSFTP(string host, string user, string password)
		{
			var client = new SftpClient(host, user, password);
			await Task.Run(() =>
			{
				client.Connect();
			});

			return client;
		}
		
	}
	
}