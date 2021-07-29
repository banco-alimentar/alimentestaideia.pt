
# Useful Database Queries
Queries that are regularly used and help debug issues and or inspect the database

## Table of contents:
- [Users](#users)
- [Donations](#donations)


## Users

- Search Users 

'SELECT TOP (100) [LoginProvider]
      ,[ProviderKey]
      ,[ProviderDisplayName]
      ,[UserId]
	  ,AspNetUsers.*

  FROM [dbo].[AspNetUserLogins]
  inner join dbo.AspNetUsers on AspNetUsers.Id=AspNetUserLogins.UserId
  where AspNetUsers.Email like 'tiago%'
  --where AspNetUsers.FullName like '%Andrade e Silva%'
  --where AspNetUsers.Nif like '%0000%''
  
- Github account

## Donations

