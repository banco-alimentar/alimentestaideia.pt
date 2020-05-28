﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Link.BA.Donate.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class BancoAlimentarEntities : DbContext
    {
        public BancoAlimentarEntities()
            : base("name=BancoAlimentarEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Donation> Donation { get; set; }
        public virtual DbSet<DonationItem> DonationItem { get; set; }
        public virtual DbSet<DonationStatus> DonationStatus { get; set; }
        public virtual DbSet<Donor> Donor { get; set; }
        public virtual DbSet<DonorAddress> DonorAddress { get; set; }
        public virtual DbSet<ProductCatalogue> ProductCatalogue { get; set; }
        public virtual DbSet<RemaxContact> RemaxContact { get; set; }
        public virtual DbSet<FoodBank> FoodBank { get; set; }
    
        public virtual ObjectResult<DonationEntity> GetDonationById(Nullable<int> donationId)
        {
            var donationIdParameter = donationId.HasValue ?
                new ObjectParameter("DonationId", donationId) :
                new ObjectParameter("DonationId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<DonationEntity>("GetDonationById", donationIdParameter);
        }
    
        public virtual ObjectResult<LastPayedDonationEntity> GetLastPayedDonations(Nullable<int> totalDonations, Nullable<int> pageNumber, Nullable<int> pageSize)
        {
            var totalDonationsParameter = totalDonations.HasValue ?
                new ObjectParameter("TotalDonations", totalDonations) :
                new ObjectParameter("TotalDonations", typeof(int));
    
            var pageNumberParameter = pageNumber.HasValue ?
                new ObjectParameter("PageNumber", pageNumber) :
                new ObjectParameter("PageNumber", typeof(int));
    
            var pageSizeParameter = pageSize.HasValue ?
                new ObjectParameter("PageSize", pageSize) :
                new ObjectParameter("PageSize", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<LastPayedDonationEntity>("GetLastPayedDonations", totalDonationsParameter, pageNumberParameter, pageSizeParameter);
        }
    
        public virtual ObjectResult<ProductCatalogueEntity> GetProductCatalogue()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<ProductCatalogueEntity>("GetProductCatalogue");
        }
    
        public virtual int InsertDonation(ObjectParameter donationId, Nullable<bool> anonym, string serviceEntity, string serviceReference, Nullable<decimal> serviceAmount, Nullable<System.DateTime> donationDate, Nullable<int> donationStatusId, Nullable<System.DateTime> donationStatusDate, string name, string email, string nIF, byte[] picture, string pictureContentType, Nullable<bool> organization, Nullable<System.DateTime> registerDate, string address1, string address2, string city, string postalCode, string phoneNumber, Nullable<int> foodBankId, Nullable<bool> wantsReceipt, string companyName)
        {
            var anonymParameter = anonym.HasValue ?
                new ObjectParameter("Anonym", anonym) :
                new ObjectParameter("Anonym", typeof(bool));
    
            var serviceEntityParameter = serviceEntity != null ?
                new ObjectParameter("ServiceEntity", serviceEntity) :
                new ObjectParameter("ServiceEntity", typeof(string));
    
            var serviceReferenceParameter = serviceReference != null ?
                new ObjectParameter("ServiceReference", serviceReference) :
                new ObjectParameter("ServiceReference", typeof(string));
    
            var serviceAmountParameter = serviceAmount.HasValue ?
                new ObjectParameter("ServiceAmount", serviceAmount) :
                new ObjectParameter("ServiceAmount", typeof(decimal));
    
            var donationDateParameter = donationDate.HasValue ?
                new ObjectParameter("DonationDate", donationDate) :
                new ObjectParameter("DonationDate", typeof(System.DateTime));
    
            var donationStatusIdParameter = donationStatusId.HasValue ?
                new ObjectParameter("DonationStatusId", donationStatusId) :
                new ObjectParameter("DonationStatusId", typeof(int));
    
            var donationStatusDateParameter = donationStatusDate.HasValue ?
                new ObjectParameter("DonationStatusDate", donationStatusDate) :
                new ObjectParameter("DonationStatusDate", typeof(System.DateTime));
    
            var nameParameter = name != null ?
                new ObjectParameter("Name", name) :
                new ObjectParameter("Name", typeof(string));
    
            var emailParameter = email != null ?
                new ObjectParameter("Email", email) :
                new ObjectParameter("Email", typeof(string));
    
            var nIFParameter = nIF != null ?
                new ObjectParameter("NIF", nIF) :
                new ObjectParameter("NIF", typeof(string));
    
            var pictureParameter = picture != null ?
                new ObjectParameter("Picture", picture) :
                new ObjectParameter("Picture", typeof(byte[]));
    
            var pictureContentTypeParameter = pictureContentType != null ?
                new ObjectParameter("PictureContentType", pictureContentType) :
                new ObjectParameter("PictureContentType", typeof(string));
    
            var organizationParameter = organization.HasValue ?
                new ObjectParameter("Organization", organization) :
                new ObjectParameter("Organization", typeof(bool));
    
            var registerDateParameter = registerDate.HasValue ?
                new ObjectParameter("RegisterDate", registerDate) :
                new ObjectParameter("RegisterDate", typeof(System.DateTime));
    
            var address1Parameter = address1 != null ?
                new ObjectParameter("Address1", address1) :
                new ObjectParameter("Address1", typeof(string));
    
            var address2Parameter = address2 != null ?
                new ObjectParameter("Address2", address2) :
                new ObjectParameter("Address2", typeof(string));
    
            var cityParameter = city != null ?
                new ObjectParameter("City", city) :
                new ObjectParameter("City", typeof(string));
    
            var postalCodeParameter = postalCode != null ?
                new ObjectParameter("PostalCode", postalCode) :
                new ObjectParameter("PostalCode", typeof(string));
    
            var phoneNumberParameter = phoneNumber != null ?
                new ObjectParameter("PhoneNumber", phoneNumber) :
                new ObjectParameter("PhoneNumber", typeof(string));
    
            var foodBankIdParameter = foodBankId.HasValue ?
                new ObjectParameter("FoodBankId", foodBankId) :
                new ObjectParameter("FoodBankId", typeof(int));
    
            var wantsReceiptParameter = wantsReceipt.HasValue ?
                new ObjectParameter("WantsReceipt", wantsReceipt) :
                new ObjectParameter("WantsReceipt", typeof(bool));
    
            var companyNameParameter = companyName != null ?
                new ObjectParameter("CompanyName", companyName) :
                new ObjectParameter("CompanyName", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("InsertDonation", donationId, anonymParameter, serviceEntityParameter, serviceReferenceParameter, serviceAmountParameter, donationDateParameter, donationStatusIdParameter, donationStatusDateParameter, nameParameter, emailParameter, nIFParameter, pictureParameter, pictureContentTypeParameter, organizationParameter, registerDateParameter, address1Parameter, address2Parameter, cityParameter, postalCodeParameter, phoneNumberParameter, foodBankIdParameter, wantsReceiptParameter, companyNameParameter);
        }
    
        public virtual int InsertDonationItem(ObjectParameter donationItemId, Nullable<int> donationId, Nullable<int> productCatalogueId, Nullable<int> quantity)
        {
            var donationIdParameter = donationId.HasValue ?
                new ObjectParameter("DonationId", donationId) :
                new ObjectParameter("DonationId", typeof(int));
    
            var productCatalogueIdParameter = productCatalogueId.HasValue ?
                new ObjectParameter("ProductCatalogueId", productCatalogueId) :
                new ObjectParameter("ProductCatalogueId", typeof(int));
    
            var quantityParameter = quantity.HasValue ?
                new ObjectParameter("Quantity", quantity) :
                new ObjectParameter("Quantity", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("InsertDonationItem", donationItemId, donationIdParameter, productCatalogueIdParameter, quantityParameter);
        }
    
        public virtual int UpdateDonationStatus(Nullable<int> donationId, Nullable<int> donationStatusId)
        {
            var donationIdParameter = donationId.HasValue ?
                new ObjectParameter("DonationId", donationId) :
                new ObjectParameter("DonationId", typeof(int));
    
            var donationStatusIdParameter = donationStatusId.HasValue ?
                new ObjectParameter("DonationStatusId", donationStatusId) :
                new ObjectParameter("DonationStatusId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateDonationStatus", donationIdParameter, donationStatusIdParameter);
        }
    
        public virtual ObjectResult<DonorsPictureEntity> GetDonorsPictureById(Nullable<int> donorId)
        {
            var donorIdParameter = donorId.HasValue ?
                new ObjectParameter("DonorId", donorId) :
                new ObjectParameter("DonorId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<DonorsPictureEntity>("GetDonorsPictureById", donorIdParameter);
        }
    
        public virtual int UpdateDonationStatusByReference(string serviceReference, Nullable<int> donationStatusId, Nullable<int> donationMode)
        {
            var serviceReferenceParameter = serviceReference != null ?
                new ObjectParameter("ServiceReference", serviceReference) :
                new ObjectParameter("ServiceReference", typeof(string));
    
            var donationStatusIdParameter = donationStatusId.HasValue ?
                new ObjectParameter("DonationStatusId", donationStatusId) :
                new ObjectParameter("DonationStatusId", typeof(int));
    
            var donationModeParameter = donationMode.HasValue ?
                new ObjectParameter("DonationMode", donationMode) :
                new ObjectParameter("DonationMode", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateDonationStatusByReference", serviceReferenceParameter, donationStatusIdParameter, donationModeParameter);
        }
    
        public virtual ObjectResult<TotalDonationsEntity> GetTotalDonations()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<TotalDonationsEntity>("GetTotalDonations");
        }
    
        public virtual ObjectResult<DonationItemsEntity> GetDonationItemsByDonationId(Nullable<int> donationId)
        {
            var donationIdParameter = donationId.HasValue ?
                new ObjectParameter("DonationId", donationId) :
                new ObjectParameter("DonationId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<DonationItemsEntity>("GetDonationItemsByDonationId", donationIdParameter);
        }
    
        public virtual ObjectResult<DonationByReferenceEntity> GetDonationByReference(string serviceReference)
        {
            var serviceReferenceParameter = serviceReference != null ?
                new ObjectParameter("ServiceReference", serviceReference) :
                new ObjectParameter("ServiceReference", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<DonationByReferenceEntity>("GetDonationByReference", serviceReferenceParameter);
        }
    
        public virtual int UpdateDonationStatusByRefAndNif(string serviceReference, string nif, Nullable<int> donationStatusId, Nullable<int> donationMode)
        {
            var serviceReferenceParameter = serviceReference != null ?
                new ObjectParameter("ServiceReference", serviceReference) :
                new ObjectParameter("ServiceReference", typeof(string));
    
            var nifParameter = nif != null ?
                new ObjectParameter("Nif", nif) :
                new ObjectParameter("Nif", typeof(string));
    
            var donationStatusIdParameter = donationStatusId.HasValue ?
                new ObjectParameter("DonationStatusId", donationStatusId) :
                new ObjectParameter("DonationStatusId", typeof(int));
    
            var donationModeParameter = donationMode.HasValue ?
                new ObjectParameter("DonationMode", donationMode) :
                new ObjectParameter("DonationMode", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateDonationStatusByRefAndNif", serviceReferenceParameter, nifParameter, donationStatusIdParameter, donationModeParameter);
        }
    
        public virtual ObjectResult<SumTotalPayedDonationEntity> GetSumTotalPayedDonation()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<SumTotalPayedDonationEntity>("GetSumTotalPayedDonation");
        }
    
        public virtual int InsertRemaxContact(ObjectParameter remaxContactId, string name, string email, string phoneNumber, string reference, string campaign)
        {
            var nameParameter = name != null ?
                new ObjectParameter("Name", name) :
                new ObjectParameter("Name", typeof(string));
    
            var emailParameter = email != null ?
                new ObjectParameter("Email", email) :
                new ObjectParameter("Email", typeof(string));
    
            var phoneNumberParameter = phoneNumber != null ?
                new ObjectParameter("PhoneNumber", phoneNumber) :
                new ObjectParameter("PhoneNumber", typeof(string));
    
            var referenceParameter = reference != null ?
                new ObjectParameter("Reference", reference) :
                new ObjectParameter("Reference", typeof(string));
    
            var campaignParameter = campaign != null ?
                new ObjectParameter("Campaign", campaign) :
                new ObjectParameter("Campaign", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("InsertRemaxContact", remaxContactId, nameParameter, emailParameter, phoneNumberParameter, referenceParameter, campaignParameter);
        }
    
        public virtual ObjectResult<RemaxContactEntity> GetRemaxContacts(string campaign)
        {
            var campaignParameter = campaign != null ?
                new ObjectParameter("Campaign", campaign) :
                new ObjectParameter("Campaign", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<RemaxContactEntity>("GetRemaxContacts", campaignParameter);
        }
    
        public virtual ObjectResult<TotalDonationValueEntity> GetTotalDonationValue()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<TotalDonationValueEntity>("GetTotalDonationValue");
        }
    
        public virtual ObjectResult<QuantitiesByProductEntity> GetQuantitiesByProduct()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<QuantitiesByProductEntity>("GetQuantitiesByProduct");
        }
    
        public virtual ObjectResult<FoodBankEntity> GetFoodBanks()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<FoodBankEntity>("GetFoodBanks");
        }
    
        public virtual ObjectResult<GetLastAepsFile_Result> GetLastAepsFile()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetLastAepsFile_Result>("GetLastAepsFile");
        }
    
        public virtual ObjectResult<GetNotPaidDonations_Result> GetNotPaidDonations()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetNotPaidDonations_Result>("GetNotPaidDonations");
        }
    
        public virtual int InsertAepsFile(string fileId, Nullable<System.DateTime> date, string fileContent)
        {
            var fileIdParameter = fileId != null ?
                new ObjectParameter("FileId", fileId) :
                new ObjectParameter("FileId", typeof(string));
    
            var dateParameter = date.HasValue ?
                new ObjectParameter("Date", date) :
                new ObjectParameter("Date", typeof(System.DateTime));
    
            var fileContentParameter = fileContent != null ?
                new ObjectParameter("FileContent", fileContent) :
                new ObjectParameter("FileContent", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("InsertAepsFile", fileIdParameter, dateParameter, fileContentParameter);
        }
    
        public virtual int InsertServiceReference(ObjectParameter reference)
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("InsertServiceReference", reference);
        }
    
        public virtual int UpdateAepsFile(string fileId, string mepsFileContent, string aepeFileContent, string aeprFileContent)
        {
            var fileIdParameter = fileId != null ?
                new ObjectParameter("FileId", fileId) :
                new ObjectParameter("FileId", typeof(string));
    
            var mepsFileContentParameter = mepsFileContent != null ?
                new ObjectParameter("MepsFileContent", mepsFileContent) :
                new ObjectParameter("MepsFileContent", typeof(string));
    
            var aepeFileContentParameter = aepeFileContent != null ?
                new ObjectParameter("AepeFileContent", aepeFileContent) :
                new ObjectParameter("AepeFileContent", typeof(string));
    
            var aeprFileContentParameter = aeprFileContent != null ?
                new ObjectParameter("AeprFileContent", aeprFileContent) :
                new ObjectParameter("AeprFileContent", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateAepsFile", fileIdParameter, mepsFileContentParameter, aepeFileContentParameter, aeprFileContentParameter);
        }
    
        public virtual ObjectResult<TotalDonationsEntity> GetTotalDonationsByFoodBank(Nullable<int> foodBank)
        {
            var foodBankParameter = foodBank.HasValue ?
                new ObjectParameter("FoodBank", foodBank) :
                new ObjectParameter("FoodBank", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<TotalDonationsEntity>("GetTotalDonationsByFoodBank", foodBankParameter);
        }
    
        public virtual ObjectResult<FoodBankEntity> GetFoodBanksByFoodBank(Nullable<int> foodBank)
        {
            var foodBankParameter = foodBank.HasValue ?
                new ObjectParameter("FoodBank", foodBank) :
                new ObjectParameter("FoodBank", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<FoodBankEntity>("GetFoodBanksByFoodBank", foodBankParameter);
        }
    
        public virtual ObjectResult<LastPayedDonationEntity> GetLastPayedDonationsByFoodBank(Nullable<int> totalDonations, Nullable<int> pageNumber, Nullable<int> pageSize, Nullable<int> foodBank)
        {
            var totalDonationsParameter = totalDonations.HasValue ?
                new ObjectParameter("TotalDonations", totalDonations) :
                new ObjectParameter("TotalDonations", typeof(int));
    
            var pageNumberParameter = pageNumber.HasValue ?
                new ObjectParameter("PageNumber", pageNumber) :
                new ObjectParameter("PageNumber", typeof(int));
    
            var pageSizeParameter = pageSize.HasValue ?
                new ObjectParameter("PageSize", pageSize) :
                new ObjectParameter("PageSize", typeof(int));
    
            var foodBankParameter = foodBank.HasValue ?
                new ObjectParameter("FoodBank", foodBank) :
                new ObjectParameter("FoodBank", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<LastPayedDonationEntity>("GetLastPayedDonationsByFoodBank", totalDonationsParameter, pageNumberParameter, pageSizeParameter, foodBankParameter);
        }
    
        public virtual int ValidateMepsFile(string fileId, string mepsFileContent)
        {
            var fileIdParameter = fileId != null ?
                new ObjectParameter("FileId", fileId) :
                new ObjectParameter("FileId", typeof(string));
    
            var mepsFileContentParameter = mepsFileContent != null ?
                new ObjectParameter("MepsFileContent", mepsFileContent) :
                new ObjectParameter("MepsFileContent", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("ValidateMepsFile", fileIdParameter, mepsFileContentParameter);
        }
    
        public virtual ObjectResult<QuantitiesByFoodBankAndProductEntity> GetQuantitiesByFoodBankAndProduct()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<QuantitiesByFoodBankAndProductEntity>("GetQuantitiesByFoodBankAndProduct");
        }
    
        public virtual ObjectResult<AllDonorsEntity> GetAllDonors()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<AllDonorsEntity>("GetAllDonors");
        }
    
        public virtual int UpdateDonationTokenByRefAndNif(string serviceReference, string nif, string token)
        {
            var serviceReferenceParameter = serviceReference != null ?
                new ObjectParameter("ServiceReference", serviceReference) :
                new ObjectParameter("ServiceReference", typeof(string));
    
            var nifParameter = nif != null ?
                new ObjectParameter("Nif", nif) :
                new ObjectParameter("Nif", typeof(string));
    
            var tokenParameter = token != null ?
                new ObjectParameter("Token", token) :
                new ObjectParameter("Token", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("UpdateDonationTokenByRefAndNif", serviceReferenceParameter, nifParameter, tokenParameter);
        }
    
        public virtual int GetProductQuantitiesByDonor()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("GetProductQuantitiesByDonor");
        }
    
        public virtual ObjectResult<GetQuantitiesByDonor_Result> GetQuantitiesByDonor()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetQuantitiesByDonor_Result>("GetQuantitiesByDonor");
        }
    }
}
