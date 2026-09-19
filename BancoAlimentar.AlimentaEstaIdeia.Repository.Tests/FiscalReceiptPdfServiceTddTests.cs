// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptPdfServiceTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Security.Cryptography;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for persistence and delivery of one immutable fiscal PDF.
    /// </summary>
    public class FiscalReceiptPdfServiceTddTests
    {
        private const string ArtifactTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptPdfArtifact";
        private const string BlobStoreTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalReceiptPdfBlobStore";
        private const string RendererTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalReceiptPdfRenderer";
        private const string ServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptPdfService";
        private const string ViewModelTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalReceiptViewModel";

        /// <summary>
        /// Rendering is isolated from blob persistence. An issued FiscalDocument persists a
        /// blob id, checksum and template version; the blob store has no delete operation.
        /// </summary>
        [Fact]
        public void FiscalReceiptPdfService_ExposesImmutableRenderPersistAndLoadContract()
        {
            Type artifactType = GetRequiredRepositoryType(ArtifactTypeName);
            Type blobStoreType = GetRequiredRepositoryType(BlobStoreTypeName);
            Type rendererType = GetRequiredRepositoryType(RendererTypeName);
            Type serviceType = GetRequiredRepositoryType(ServiceTypeName);
            Type viewModelType = GetRequiredRepositoryType(ViewModelTypeName);

            MethodInfo render = Assert.Single(
                rendererType.GetMethods(),
                method => method.Name == "Render" &&
                    method.ReturnType == typeof(byte[]) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { viewModelType }));
            Assert.Single(
                blobStoreType.GetMethods(),
                method => method.Name == "Save" &&
                    method.ReturnType == typeof(void) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(Guid), typeof(byte[]) }));
            Assert.Single(
                blobStoreType.GetMethods(),
                method => method.Name == "Load" &&
                    method.ReturnType == typeof(byte[]) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(Guid) }));
            Assert.DoesNotContain(blobStoreType.GetMethods(), method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(serviceType.GetConstructor(new[] { rendererType, blobStoreType }));
            Assert.Single(
                serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "GetOrCreate" &&
                    method.ReturnType == artifactType &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(FiscalDocument), viewModelType }));
            AssertRequiredProperties(artifactType, "BlobName", "Checksum", "Content", "TemplateVersion");
            Assert.Equal(typeof(string), typeof(FiscalDocument).GetProperty("PdfTemplateVersion")?.PropertyType);
            Assert.Equal("Render", render.Name);
        }

        /// <summary>
        /// The first call renders exactly once, saves a deterministic blob, and records the
        /// SHA-256 checksum. Subsequent calls return the same persisted bytes even if a caller
        /// presents a newer template model.
        /// </summary>
        [Fact]
        public void GetOrCreate_RendersOncePersistsChecksumAndReturnsSameIssuedArtifact()
        {
            Type rendererType = GetRequiredRepositoryType(RendererTypeName);
            Type storeType = GetRequiredRepositoryType(BlobStoreTypeName);
            TestPdfRenderer renderer = CreateRenderer(rendererType, out object rendererProxy);
            TestPdfBlobStore store = CreateBlobStore(storeType, out object storeProxy);
            object service = CreateService(rendererProxy, storeProxy);
            FiscalDocument document = CreateIssuedDocument(templateVersion: "fiscal-v1");
            object originalViewModel = CreateViewModel(templateVersion: "fiscal-v1");

            object firstArtifact = GetOrCreate(service, document, originalViewModel);
            Guid persistedBlobName = Assert.IsType<Guid>(GetRequiredProperty(firstArtifact, "BlobName"));
            string expectedChecksum = Convert.ToHexString(SHA256.HashData(TestPdfRenderer.PdfBytes));

            Assert.Equal(persistedBlobName, document.PdfBlobName);
            Assert.Equal(expectedChecksum, document.PdfChecksum);
            Assert.Equal("fiscal-v1", GetRequiredStringProperty(firstArtifact, "TemplateVersion"));
            Assert.Equal(TestPdfRenderer.PdfBytes, Assert.IsType<byte[]>(GetRequiredProperty(firstArtifact, "Content")));
            Assert.Single(renderer.TemplateVersions);
            Assert.Single(store.SavedBlobNames);
            Assert.Equal(persistedBlobName, Assert.Single(store.SavedBlobNames));

            object changedViewModel = CreateViewModel(templateVersion: "fiscal-v2");
            object repeatedArtifact = GetOrCreate(service, document, changedViewModel);

            Assert.Equal(persistedBlobName, Assert.IsType<Guid>(GetRequiredProperty(repeatedArtifact, "BlobName")));
            Assert.Equal(expectedChecksum, GetRequiredStringProperty(repeatedArtifact, "Checksum"));
            Assert.Equal(TestPdfRenderer.PdfBytes, Assert.IsType<byte[]>(GetRequiredProperty(repeatedArtifact, "Content")));
            Assert.Single(renderer.TemplateVersions);
            Assert.Single(store.SavedBlobNames);
            Assert.Equal(1, store.LoadedBlobNames.Count(name => name == persistedBlobName));
        }

        /// <summary>
        /// A failed save leaves no document artifact pointers. Recovery must use the persisted
        /// template version; a different live template model is rejected before rendering, and
        /// recovery with the persisted version produces the first and only artifact.
        /// </summary>
        [Fact]
        public void GetOrCreate_RecoversOnlyWithPersistedTemplateVersionAfterSaveFailure()
        {
            Type rendererType = GetRequiredRepositoryType(RendererTypeName);
            Type storeType = GetRequiredRepositoryType(BlobStoreTypeName);
            TestPdfRenderer renderer = CreateRenderer(rendererType, out object rendererProxy);
            TestPdfBlobStore store = CreateBlobStore(storeType, out object storeProxy);
            store.ExceptionToThrow = new InvalidOperationException("Blob storage unavailable.");
            object service = CreateService(rendererProxy, storeProxy);
            FiscalDocument document = CreateIssuedDocument(templateVersion: "fiscal-v1");

            InvalidOperationException saveException = Assert.Throws<InvalidOperationException>(
                () => GetOrCreate(service, document, CreateViewModel(templateVersion: "fiscal-v1")));

            Assert.Contains("blob", saveException.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Null(document.PdfBlobName);
            Assert.Null(document.PdfChecksum);

            store.ExceptionToThrow = null;
            InvalidOperationException templateException = Assert.Throws<InvalidOperationException>(
                () => GetOrCreate(service, document, CreateViewModel(templateVersion: "fiscal-v2")));

            Assert.Contains("template", templateException.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(renderer.TemplateVersions);
            Assert.Empty(store.SavedBlobNames);

            object recoveredArtifact = GetOrCreate(service, document, CreateViewModel(templateVersion: "fiscal-v1"));

            Assert.Equal("fiscal-v1", GetRequiredStringProperty(recoveredArtifact, "TemplateVersion"));
            Assert.NotNull(document.PdfBlobName);
            Assert.NotNull(document.PdfChecksum);
            Assert.Equal(2, renderer.TemplateVersions.Count);
            Assert.Single(store.SavedBlobNames);
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static TestPdfRenderer CreateRenderer(Type rendererType, out object rendererProxy)
        {
            rendererProxy = DispatchProxy.Create(rendererType, typeof(TestPdfRenderer));
            return Assert.IsAssignableFrom<TestPdfRenderer>(rendererProxy);
        }

        private static TestPdfBlobStore CreateBlobStore(Type storeType, out object storeProxy)
        {
            storeProxy = DispatchProxy.Create(storeType, typeof(TestPdfBlobStore));
            return Assert.IsAssignableFrom<TestPdfBlobStore>(storeProxy);
        }

        private static object CreateService(object renderer, object blobStore)
        {
            Type serviceType = GetRequiredRepositoryType(ServiceTypeName);
            Type rendererType = GetRequiredRepositoryType(RendererTypeName);
            Type storeType = GetRequiredRepositoryType(BlobStoreTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { rendererType, storeType });
            Assert.NotNull(constructor);
            return constructor.Invoke(new[] { renderer, blobStore });
        }

        private static FiscalDocument CreateIssuedDocument(string templateVersion)
        {
            var document = new FiscalDocument
            {
                Id = 42,
                DocumentNumber = "RC 2026/42",
                Status = FiscalDocumentStatus.Issued,
            };
            SetRequiredProperty(document, "PdfTemplateVersion", templateVersion);
            return document;
        }

        private static object CreateViewModel(string templateVersion)
        {
            Type viewModelType = GetRequiredRepositoryType(ViewModelTypeName);
            object viewModel = Activator.CreateInstance(viewModelType);
            SetRequiredProperty(viewModel, "TemplateVersion", templateVersion);
            return viewModel;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalSigningKeyProvider).Assembly.GetType(typeName));
        }

        private static object GetOrCreate(object service, FiscalDocument document, object viewModel)
        {
            Type viewModelType = GetRequiredRepositoryType(ViewModelTypeName);
            MethodInfo method = service.GetType().GetMethod("GetOrCreate", new[] { typeof(FiscalDocument), viewModelType });
            Assert.NotNull(method);

            try
            {
                object artifact = method.Invoke(service, new[] { document, viewModel });
                Assert.NotNull(artifact);
                return artifact;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static object GetRequiredProperty(object source, string propertyName)
        {
            PropertyInfo property = source.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            object value = property.GetValue(source);
            Assert.NotNull(value);
            return value;
        }

        private static string GetRequiredStringProperty(object source, string propertyName)
        {
            return Assert.IsType<string>(GetRequiredProperty(source, propertyName));
        }

        private static void SetRequiredProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(target, value);
        }

        private class TestPdfRenderer : DispatchProxy
        {
            public static readonly byte[] PdfBytes = { 37, 80, 68, 70, 45, 49, 46, 55 };

            public IList<string> TemplateVersions { get; } = new List<string>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Render")
                {
                    throw new NotSupportedException($"Unexpected PDF renderer method: {targetMethod.Name}.");
                }

                object viewModel = Assert.Single(args);
                this.TemplateVersions.Add(GetRequiredStringProperty(viewModel, "TemplateVersion"));
                return PdfBytes;
            }
        }

        private class TestPdfBlobStore : DispatchProxy
        {
            private readonly IDictionary<Guid, byte[]> blobs = new Dictionary<Guid, byte[]>();

            public Exception ExceptionToThrow { get; set; }

            public IList<Guid> LoadedBlobNames { get; } = new List<Guid>();

            public IList<Guid> SavedBlobNames { get; } = new List<Guid>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "Save")
                {
                    Guid blobName = Assert.IsType<Guid>(args[0]);
                    byte[] content = Assert.IsType<byte[]>(args[1]);
                    if (this.ExceptionToThrow != null)
                    {
                        ExceptionDispatchInfo.Capture(this.ExceptionToThrow).Throw();
                    }

                    this.blobs.Add(blobName, content);
                    this.SavedBlobNames.Add(blobName);
                    return null;
                }

                if (targetMethod.Name == "Load")
                {
                    Guid blobName = Assert.IsType<Guid>(Assert.Single(args));
                    this.LoadedBlobNames.Add(blobName);
                    return this.blobs.TryGetValue(blobName, out byte[] content) ? content : null;
                }

                throw new NotSupportedException($"Unexpected PDF blob store method: {targetMethod.Name}.");
            }
        }
    }
}
