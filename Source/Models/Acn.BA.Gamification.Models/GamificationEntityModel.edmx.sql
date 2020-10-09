
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 10/07/2020 12:28:27
-- Generated from EDMX file: C:\projects\BancoAlimentar\alimentestaideia.pt\Source\Models\Acn.BA.Gamification.Models\GamificationEntityModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [ba-gamification];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------


-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'CompletedDonationSet'
CREATE TABLE [dbo].[CompletedDonationSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Email] nvarchar(max)  NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Amount] decimal(18,0)  NOT NULL,
    [User1Name] nvarchar(max)  NOT NULL,
    [User1Email] nvarchar(max)  NOT NULL,
    [User2Name] nvarchar(max)  NOT NULL,
    [User2Email] nvarchar(max)  NOT NULL,
    [User3Name] nvarchar(max)  NOT NULL,
    [User3Email] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'DonationSet'
CREATE TABLE [dbo].[DonationSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] int  NOT NULL,
    [Amount] decimal(18,0)  NOT NULL
);
GO

-- Creating table 'UserSet'
CREATE TABLE [dbo].[UserSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Email] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'InviteSet'
CREATE TABLE [dbo].[InviteSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [FromUserId] int  NOT NULL,
    [ToUserId] int  NOT NULL,
    [Nickname] nvarchar(max)  NOT NULL,
    [LastPokeTs] nvarchar(max)  NOT NULL,
    [DonationId] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'CompletedDonationSet'
ALTER TABLE [dbo].[CompletedDonationSet]
ADD CONSTRAINT [PK_CompletedDonationSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'DonationSet'
ALTER TABLE [dbo].[DonationSet]
ADD CONSTRAINT [PK_DonationSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'UserSet'
ALTER TABLE [dbo].[UserSet]
ADD CONSTRAINT [PK_UserSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'InviteSet'
ALTER TABLE [dbo].[InviteSet]
ADD CONSTRAINT [PK_InviteSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [UserId] in table 'DonationSet'
ALTER TABLE [dbo].[DonationSet]
ADD CONSTRAINT [FK_UserDonation]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[UserSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserDonation'
CREATE INDEX [IX_FK_UserDonation]
ON [dbo].[DonationSet]
    ([UserId]);
GO

-- Creating foreign key on [FromUserId] in table 'InviteSet'
ALTER TABLE [dbo].[InviteSet]
ADD CONSTRAINT [FK_UserInvite]
    FOREIGN KEY ([FromUserId])
    REFERENCES [dbo].[UserSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserInvite'
CREATE INDEX [IX_FK_UserInvite]
ON [dbo].[InviteSet]
    ([FromUserId]);
GO

-- Creating foreign key on [ToUserId] in table 'InviteSet'
ALTER TABLE [dbo].[InviteSet]
ADD CONSTRAINT [FK_UserInvite1]
    FOREIGN KEY ([ToUserId])
    REFERENCES [dbo].[UserSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_UserInvite1'
CREATE INDEX [IX_FK_UserInvite1]
ON [dbo].[InviteSet]
    ([ToUserId]);
GO

-- Creating foreign key on [DonationId] in table 'InviteSet'
ALTER TABLE [dbo].[InviteSet]
ADD CONSTRAINT [FK_DonationInvite]
    FOREIGN KEY ([DonationId])
    REFERENCES [dbo].[DonationSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_DonationInvite'
CREATE INDEX [IX_FK_DonationInvite]
ON [dbo].[InviteSet]
    ([DonationId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------