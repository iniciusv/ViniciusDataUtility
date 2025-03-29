using DataUtility3.Transformers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGroundTransformer;

public class UserMapper : HeaderMapper<User>
{
	private readonly IEnumerable<Profile> _availableProfiles;

	public UserMapper(string group, IEnumerable<Profile> availableProfiles) : base("UserMapper")
	{
		_availableProfiles = availableProfiles ?? throw new ArgumentNullException(nameof(availableProfiles));

		Map("GUID", u => u.GUID);
		Map("Created", u => u.Created);
		Map("ClientCode", u => u.ClientCode);
		Map("Description", u => u.Description);
		Map("NCM", u => u.NCM);
		Map("Profile", u => u.Profile); // Mapeia diretamente para a propriedade Profile

		SetStaticValue(u => u.Group, group);

		// Configura o resolvedor de referência para o cabeçalho "Profile"
		MapReference(
			headerName: "Profile",
			referenceSource: profileName => _availableProfiles,
			matchPredicate: (profileName, profile) =>
				profile.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)
		);
	}
}