﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Umbraco.Core.Models
{
    /// <summary>
    /// Represents the content type that a <see cref="Member"/> object is based on
    /// </summary>
    [Serializable]
    [DataContract(IsReference = true)]
    public class MemberType : ContentTypeCompositionBase, IMemberType
    {
        private static readonly Lazy<PropertySelectors> Ps = new Lazy<PropertySelectors>();
        public const bool IsPublishingConst = false;

        //Dictionary is divided into string: PropertyTypeAlias, Tuple: MemberCanEdit, VisibleOnProfile, PropertyTypeId
        private string _alias;

        public MemberType(int parentId) : base(parentId)
        {
            MemberTypePropertyTypes = new Dictionary<string, MemberTypePropertyProfileAccess>();
        }

        public MemberType(IContentTypeComposition parent) : this(parent, null)
        {
        }

        public MemberType(IContentTypeComposition parent, string alias)
            : base(parent, alias)
        {
            MemberTypePropertyTypes = new Dictionary<string, MemberTypePropertyProfileAccess>();
        }

        /// <inheritdoc />
        public override bool IsPublishing => IsPublishingConst;

        public override ContentVariation Variations
        {
            // note: although technically possible, variations on members don't make much sense
            // and therefore are disabled - they are fully supported at service level, though,
            // but not at published snapshot level.

            get => base.Variations;
            set => throw new NotSupportedException("Variations are not supported on members.");
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class PropertySelectors
        {
            public readonly PropertyInfo AliasSelector = ExpressionHelper.GetPropertyInfo<MemberType, string>(x => x.Alias);
        }

        /// <summary>
        /// The Alias of the ContentType
        /// </summary>
        [DataMember]
        public override string Alias
        {
            get => _alias;
            set
            {
                //NOTE: WE are overriding this because we don't want to do a ToSafeAlias when the alias is the special case of
                // "_umbracoSystemDefaultProtectType" which is used internally, currently there is an issue with the safe alias as it strips
                // leading underscores which we don't want in this case.
                // see : http://issues.umbraco.org/issue/U4-3968

                //TODO: BUT, I'm pretty sure we could do this with regards to underscores now:
                // .ToCleanString(CleanStringType.Alias | CleanStringType.UmbracoCase)
                // Need to ask Stephen

                var newVal = value == "_umbracoSystemDefaultProtectType"
                        ? value
                        : (value == null ? string.Empty : value.ToSafeAlias());

                SetPropertyValueAndDetectChanges(newVal, ref _alias, Ps.Value.AliasSelector);
            }
        }

        /// <summary>
        /// Gets or Sets a Dictionary of Tuples (MemberCanEdit, VisibleOnProfile) by the PropertyTypes' alias.
        /// </summary>
        [DataMember]
        internal IDictionary<string, MemberTypePropertyProfileAccess> MemberTypePropertyTypes { get; private set; }

        /// <summary>
        /// Gets a boolean indicating whether a Property is editable by the Member.
        /// </summary>
        /// <param name="propertyTypeAlias">PropertyType Alias of the Property to check</param>
        /// <returns></returns>
        public bool MemberCanEditProperty(string propertyTypeAlias)
        {
            return MemberTypePropertyTypes.ContainsKey(propertyTypeAlias) && MemberTypePropertyTypes[propertyTypeAlias].IsEditable;
        }

        /// <summary>
        /// Gets a boolean indicating whether a Property is visible on the Members profile.
        /// </summary>
        /// <param name="propertyTypeAlias">PropertyType Alias of the Property to check</param>
        /// <returns></returns>
        public bool MemberCanViewProperty(string propertyTypeAlias)
        {
            return MemberTypePropertyTypes.ContainsKey(propertyTypeAlias) && MemberTypePropertyTypes[propertyTypeAlias].IsVisible;
        }

        /// <summary>
        /// Sets a boolean indicating whether a Property is editable by the Member.
        /// </summary>
        /// <param name="propertyTypeAlias">PropertyType Alias of the Property to set</param>
        /// <param name="value">Boolean value, true or false</param>
        public void SetMemberCanEditProperty(string propertyTypeAlias, bool value)
        {
            if (MemberTypePropertyTypes.ContainsKey(propertyTypeAlias))
            {
                MemberTypePropertyTypes[propertyTypeAlias].IsEditable = value;
            }
            else
            {
                var tuple = new MemberTypePropertyProfileAccess(false, value);
                MemberTypePropertyTypes.Add(propertyTypeAlias, tuple);
            }
        }

        /// <summary>
        /// Sets a boolean indicating whether a Property is visible on the Members profile.
        /// </summary>
        /// <param name="propertyTypeAlias">PropertyType Alias of the Property to set</param>
        /// <param name="value">Boolean value, true or false</param>
        public void SetMemberCanViewProperty(string propertyTypeAlias, bool value)
        {
            if (MemberTypePropertyTypes.ContainsKey(propertyTypeAlias))
            {
                MemberTypePropertyTypes[propertyTypeAlias].IsVisible = value;
            }
            else
            {
                var tuple = new MemberTypePropertyProfileAccess(value, false);
                MemberTypePropertyTypes.Add(propertyTypeAlias, tuple);
            }
        }
    }
}
