﻿BEGIN TRY
    CREATE TABLE [dbo].[DataSets]
    (
	    [datasetId] INT IDENTITY(1,1) PRIMARY KEY, 
        [userId] INT NULL, 
        [label] NVARCHAR(64) NOT NULL, 
        [tableName] NVARCHAR(64) NOT NULL, 
        [partialview] NVARCHAR(255) NOT NULL DEFAULT '', 
        [datecreated] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), 
        [description] NVARCHAR(MAX) NOT NULL, 
        [userdata] BIT NOT NULL DEFAULT 0,
        [deleted] BIT NOT NULL DEFAULT 0
    )
END TRY
BEGIN CATCH END CATCH

GO

BEGIN TRY
    CREATE INDEX [IX_DataSets_TableName] ON [dbo].[DataSets] ([tableName])
END TRY
BEGIN CATCH END CATCH
