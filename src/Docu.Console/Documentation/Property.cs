using System;
using System.Collections.Generic;
using System.Reflection;
using Docu.Parsing.Model;

namespace Docu.Documentation
{
    public class Property : BaseDocumentationElement, IReferencable
    {
        public Property(PropertyIdentifier identifier, DeclaredType type)
            : base(identifier)
        {
            Type = type;
            HasGet = identifier.HasGet;
            HasSet = identifier.HasSet;
        }

        PropertyInfo declaration;

        public bool HasGet { get; private set; }
        public bool HasSet { get; private set; }

        public IReferencable ReturnType { get; set; }
        public DeclaredType Type { get; set; }

        public string FullName
        {
            get { return Name; }
        }

        public string PrettyName
        {
            get { return Name; }
        }

        public void Resolve(IDictionary<Identifier, IReferencable> referencables)
        {
            if (referencables.ContainsKey(identifier))
            {
                IsResolved = true;
                IReferencable referencable = referencables[identifier];
                var property = referencable as Property;

                if (property == null)
                {
                    throw new InvalidOperationException("Cannot resolve to '" + referencable.GetType().FullName + "'");
                }

                ReturnType = property.ReturnType;

                if (!ReturnType.IsResolved)
                {
                    ReturnType.Resolve(referencables);
                }

                declaration = property.declaration;
                try {
                    if (declaration != null && declaration.IsDefined(typeof(ObsoleteAttribute))) {
                        ObsoleteReason = declaration.GetCustomAttribute<ObsoleteAttribute>().Message;
                    }

                }
                catch (Exception ex) {
                    Console.WriteLine("Cannot perform check for custom attributes");
                    ConvertToExternalReference();
                }
                if (!Summary.IsResolved)
                {
                    Summary.Resolve(referencables);
                }

                if (!Remarks.IsResolved)
                {
                    Remarks.Resolve(referencables);
                }
            }
            else
            {
                ConvertToExternalReference();
            }
        }

        public static Property Unresolved(PropertyIdentifier propertyIdentifier, DeclaredType type)
        {
            return new Property(propertyIdentifier, type) {IsResolved = false};
        }

        public static Property Unresolved(PropertyIdentifier propertyIdentifier, DeclaredType type, PropertyInfo declaration, IReferencable returnType)
        {
            return new Property(propertyIdentifier, type) {IsResolved = false, declaration = declaration, ReturnType = returnType};
        }
    }
}
