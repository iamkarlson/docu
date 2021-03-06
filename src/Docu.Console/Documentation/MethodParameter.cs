namespace Docu.Documentation
{
    public class MethodParameter : BaseDocumentationElement, IReferrer
    {
        public MethodParameter(string name, IReferencable reference)
            : base(new NullIdentifier(name))
        {
            Reference = reference;
        }

        public string PrettyName
        {
            get
            {
                var typeReference = Reference as DeclaredType;

                if (typeReference != null)
                {
                    return typeReference.PrettyName;
                }

                var methodReference = Reference as Method;

                if (methodReference != null)
                {
                    return methodReference.PrettyName;
                }

                // var externalReference = Reference as ExternalReference;

                // if (externalReference != null)
                // return externalReference.PrettyName;
                return Name;
            }
        }

        public IReferencable Reference { get; set; }
    }
}
