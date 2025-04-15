using DataUtility.DataBase.DataBases;
using DataUtility.Domain;
using System.Transactions;

namespace DataBaseCopier;
class Program
{
	static void Main(string[] args)
	{
		//const string sourceConnStr = "Data Source=tars.database.windows.net;Initial Catalog=ambev-poc;Persist Security Info=True;User ID=tereos;Password=WePi_r_sW&wrUNUsu9i@;Connection Timeout=10000;MultipleActiveResultSets=true;";
		//const string sourceConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=ambev-poc;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";
		const string sourceConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=sylvamo-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

		const string targetConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=ecorodovias-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

		const string TereosConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=Tereos;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

		const string SbfQAConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=slartibartfast-qa;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";

		//const string targetConnStr = "Server=tcp:tars.database.windows.net;Initial Catalog=bid-poc;Persist Security Info=False;User ID=viniciusfortes@42codelab.com;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=Active Directory Interactive;";



		// Configurações fixas
		const string tableName = "IVADefinitionPolicy";
		const string condition = "TenantID =7"; // Nova cláusula WHERE
												//const string condition = "ClientCode = 0206045996"; // Nova cláusula WHERE
		const bool replaceExisting = false;


		var copier = new DatabaseCopierService();
		copier.CopyDataBetweenDatabases(
			SbfQAConnStr,
			SbfQAConnStr,
			tableName,
			condition,
			replaceExisting
		);
	}
}