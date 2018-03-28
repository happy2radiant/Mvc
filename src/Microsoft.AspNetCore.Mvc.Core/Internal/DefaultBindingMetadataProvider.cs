// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A default implementation of <see cref="IBindingMetadataProvider"/>.
    /// </summary>
    public class DefaultBindingMetadataProvider : IBindingMetadataProvider
    {
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var isBindingInfoPresent = false;

            // BinderModelName
            foreach (var binderModelNameAttribute in context.Attributes.OfType<IModelNameProvider>())
            {
                isBindingInfoPresent = true;
                if (binderModelNameAttribute?.Name != null)
                {
                    context.BindingMetadata.BinderModelName = binderModelNameAttribute.Name;
                    break;
                }
            }

            // BinderType
            foreach (var binderTypeAttribute in context.Attributes.OfType<IBinderTypeProviderMetadata>())
            {
                isBindingInfoPresent = true;
                if (binderTypeAttribute.BinderType != null)
                {
                    context.BindingMetadata.BinderType = binderTypeAttribute.BinderType;
                    break;
                }
            }

            // BindingSource
            foreach (var bindingSourceAttribute in context.Attributes.OfType<IBindingSourceMetadata>())
            {
                isBindingInfoPresent = true;
                if (bindingSourceAttribute.BindingSource != null)
                {
                    context.BindingMetadata.BindingSource = bindingSourceAttribute.BindingSource;
                    break;
                }
            }

            // HasBindingMetadata is true if the 
            if (isBindingInfoPresent)
            {
                context.BindingMetadata.HasBindingMetadata = true;
            }

            // PropertyFilterProvider
            var propertyFilterProviders = context.Attributes.OfType<IPropertyFilterProvider>().ToArray();

            if (propertyFilterProviders.Length == 0)
            {
                context.BindingMetadata.PropertyFilterProvider = null;
            }
            else if (propertyFilterProviders.Length == 1)
            {
                isBindingInfoPresent = true;
                context.BindingMetadata.PropertyFilterProvider = propertyFilterProviders[0];
            }
            else
            {
                isBindingInfoPresent = true;
                var composite = new CompositePropertyFilterProvider(propertyFilterProviders);
                context.BindingMetadata.PropertyFilterProvider = composite;
            }

            var bindingBehavior = FindBindingBehavior(context);
            if (bindingBehavior != null)
            {
                // Note: We intentionally do not include the presence of BindingBehavrioAttribute to calculate
                // isBindingInfoPresent. This is match the behavior of BindingInfo.GetBindingInfo() that did not inspect
                // this attribute. This additionally ensures that parameters and properties annotated with 
                // such as BindRequiredAttribute \ BindNeverAttribute, do not get model bound unless they explicitly have
                // a different model binding related attribute.
                context.BindingMetadata.IsBindingAllowed = bindingBehavior.Behavior != BindingBehavior.Never;
                context.BindingMetadata.IsBindingRequired = bindingBehavior.Behavior == BindingBehavior.Required;
            }

            // RequestPredicateProvider
            foreach (var requestPredicateProvider in context.Attributes.OfType<IRequestPredicateProvider>())
            {
                isBindingInfoPresent = true;
                if (requestPredicateProvider.RequestPredicate != null)
                {
                    context.BindingMetadata.RequestPredicate = requestPredicateProvider.RequestPredicate;
                    break;
                }
            }

            if (isBindingInfoPresent)
            {
                context.BindingMetadata.HasBindingMetadata = isBindingInfoPresent;
            }
        }

        private static BindingBehaviorAttribute FindBindingBehavior(BindingMetadataProviderContext context)
        {
            switch (context.Key.MetadataKind)
            {
                case ModelMetadataKind.Property:
                    // BindingBehavior can fall back to attributes on the Container Type, but we should ignore
                    // attributes on the Property Type.
                    var matchingAttributes = context.PropertyAttributes.OfType<BindingBehaviorAttribute>();
                    return matchingAttributes.FirstOrDefault()
                        ?? context.Key.ContainerType.GetTypeInfo()
                            .GetCustomAttributes(typeof(BindingBehaviorAttribute), inherit: true)
                            .OfType<BindingBehaviorAttribute>()
                            .FirstOrDefault();
                case ModelMetadataKind.Parameter:
                    return context.ParameterAttributes.OfType<BindingBehaviorAttribute>().FirstOrDefault();
                default:
                    return null;
            }
        }

        private class CompositePropertyFilterProvider : IPropertyFilterProvider
        {
            private readonly IEnumerable<IPropertyFilterProvider> _providers;

            public CompositePropertyFilterProvider(IEnumerable<IPropertyFilterProvider> providers)
            {
                _providers = providers;
            }

            public Func<ModelMetadata, bool> PropertyFilter => CreatePropertyFilter();

            private Func<ModelMetadata, bool> CreatePropertyFilter()
            {
                var propertyFilters = _providers
                    .Select(p => p.PropertyFilter)
                    .Where(p => p != null);

                return (m) =>
                {
                    foreach (var propertyFilter in propertyFilters)
                    {
                        if (!propertyFilter(m))
                        {
                            return false;
                        }
                    }

                    return true;
                };
            }
        }
    }
}