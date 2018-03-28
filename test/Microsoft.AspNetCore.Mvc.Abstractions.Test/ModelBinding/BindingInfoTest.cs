// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class BindingInfoTest
    {
        [Fact]
        public void GetBindingInfo_WithAttributes_ConstructsBindingInfo()
        {
            // Arrange
            var attributes = new object[] 
            {
                new FromQueryAttribute { Name = "Test" },
            };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same("Test", bindingInfo.BinderModelName);
            Assert.Same(BindingSource.Query, bindingInfo.BindingSource);
        }

        [Fact]
        public void GetBindingInfo_ReturnsNull_IfNoBindingAttributesArePresent()
        {
            // Arrange
            var attributes = new object[] { new  ControllerAttribute(), new BindNeverAttribute(), };

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            // Assert
            Assert.Null(bindingInfo);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesValuesFromBindingInfo_IfAttributesPresent()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute { BinderType = typeof(object), Name = "Test" },
            };
            var modelAttributes = new ModelAttributes(Enumerable.Empty<object>(), null, null);
            var metadataDetails = new DefaultMetadataDetails(ModelMetadataIdentity.ForType(typeof(object)), modelAttributes)
            {
                BindingMetadata = new BindingMetadata
                {
                    BindingSource = BindingSource.Special,
                    BinderType = typeof(string),
                    BinderModelName = "Different",
                },
            };
            var modelMetadata = new DefaultModelMetadata(new EmptyModelMetadataProvider(), Mock.Of<ICompositeMetadataDetailsProvider>(), metadataDetails);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(typeof(object), bindingInfo.BinderType);
            Assert.Same("Test", bindingInfo.BinderModelName);
        }

        [Fact]
        public void GetBindingInfo_WithAttributesAndModelMetadata_UsesValuesFromModelMetadata_IfNoBindingAttributesArePresent()
        {
            // Arrange
            var attributes = new object[] { new ControllerAttribute(), new BindNeverAttribute(), };
            var modelAttributes = new ModelAttributes(Enumerable.Empty<object>(), null, null);
            var metadataDetails = new DefaultMetadataDetails(ModelMetadataIdentity.ForType(typeof(object)), modelAttributes)
            {
                BindingMetadata = new BindingMetadata
                {
                    BindingSource = BindingSource.Special,
                    BinderType = typeof(string),
                    BinderModelName = "Different",
                },
            };
            var modelMetadata = new DefaultModelMetadata(new EmptyModelMetadataProvider(), Mock.Of<ICompositeMetadataDetailsProvider>(), metadataDetails);

            // Act
            var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

            // Assert
            Assert.NotNull(bindingInfo);
            Assert.Same(typeof(string), bindingInfo.BinderType);
            Assert.Same("Different", bindingInfo.BinderModelName);
            Assert.Same(BindingSource.Special, bindingInfo.BindingSource);
        }
    }
}
