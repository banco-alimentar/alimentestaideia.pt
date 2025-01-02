
# Useful Database Queries
Queries that are regularly used and help debug issues and or inspect the database

## Table of contents:
- [Users](#users)
- [Donations](#donations)


## Users

- Search Users and their login information filtered by email, fullname or NIF

```
SELECT TOP (100) [LoginProvider]
      ,[ProviderKey]
      ,[ProviderDisplayName]
      ,[UserId]
	  ,AspNetUsers.*

  FROM [dbo].[AspNetUserLogins]
  inner join dbo.AspNetUsers on AspNetUsers.Id=AspNetUserLogins.UserId
  where AspNetUsers.Email like 'tiago%'
  --where AspNetUsers.FullName like '%Andrade e Silva%'
  --where AspNetUsers.Nif like '%0000%'

```  
  
- Search user by id
```
select * from AspNetUsers where id='4f51bff7-a1fe-467f-8a34-5a14e54fbffd'
```

## Donations, payments and invoices

- user info and Paid donations who want receipt
```
SELECT        TOP (30) dbo.AspNetUsers.NIF, dbo.AspNetUsers.FullName, dbo.AspNetUsers.Id, dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.WantsReceipt, dbo.Donations.PaymentStatus, 
                         dbo.Donations.PublicId
FROM            dbo.AspNetUsers RIGHT OUTER JOIN
                         dbo.Donations ON dbo.AspNetUsers.Id = dbo.Donations.UserId
WHERE        (dbo.Donations.WantsReceipt = 1) AND (dbo.Donations.PaymentStatus = 1)
ORDER BY dbo.Donations.DonationDate DESC
```

- donations by User NIF
```
SELECT        donations.id,dbo.AspNetUsers.NIF, dbo.AspNetUsers.id, dbo.AspNetUsers.FullName, dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.WantsReceipt, dbo.Invoices.Sequence, dbo.Invoices.Number,
                         dbo.Invoices.DonationId, dbo.Invoices.Created
FROM            dbo.Invoices INNER JOIN
                         dbo.Donations ON dbo.Invoices.Id = dbo.Donations.Id INNER JOIN
                         dbo.AspNetUsers ON dbo.Donations.UserId = dbo.AspNetUsers.Id
WHERE       
	dbo.AspNetUsers.NIF = N'195827345'
order by DonationDate desc
```


