namespace Purview.SourceGeneratorFramework.Generators.Helpers;

static class GeneratorTypeLibrary
{
	const string GeneratorsNamespace = "Purview.SourceGeneratorFramework.Generators";

	public static readonly TypeIdentity TypeIdentity = TypeIdentity.Create<TypeIdentity>();

	public static readonly TypeIdentity TypeReference = TypeIdentity.Create<TypeReference>();

	public static readonly TypeIdentity EnumValueDefinition = TypeIdentity.Create<EnumValueDefinition>();

	public static class Attirbutes
	{
		public static readonly TypeIdentity GenerateAttribute = new(nameof(GenerateAttribute), GeneratorsNamespace);

		public static readonly TypeIdentity PropertyAttribute = new(nameof(PropertyAttribute), GeneratorsNamespace);

		public static readonly TypeIdentity ArgumentAttribute = new(nameof(ArgumentAttribute), GeneratorsNamespace);

		public static readonly TypeIdentity NestedModelAttribute = new(
			nameof(NestedModelAttribute),
			GeneratorsNamespace
		);

		public static readonly TypeIdentity ExcludeAttribute = new(nameof(ExcludeAttribute), GeneratorsNamespace);

		public static readonly TypeIdentity GenericTypeArgumentAttribute = new(
			nameof(GenericTypeArgumentAttribute),
			GeneratorsNamespace
		);

		public static readonly TypeIdentity GenerateTypeLibraryAttribute = new(
			nameof(GenerateTypeLibraryAttribute),
			GeneratorsNamespace
		);

		public static readonly TypeIdentity TypeRefAttribute = new(nameof(TypeRefAttribute), GeneratorsNamespace);

		public static readonly TypeIdentity EnumValueAttribute = new(nameof(EnumValueAttribute), GeneratorsNamespace);
	}
}
