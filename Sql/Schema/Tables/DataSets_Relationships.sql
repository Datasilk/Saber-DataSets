BEGIN TRY
    CREATE TABLE [dbo].[DataSets_Relationships]
    (
	    [parentId] INT NOT NULL, 
        [childId] INT NOT NULL, 
        [parentList] NVARCHAR(32) NOT NULL, 
        [childColumn] NVARCHAR(32) NOT NULL DEFAULT '', 
        [listtype] INT NOT NULL DEFAULT 1,
        PRIMARY KEY ([parentId], [childId])
    )
END TRY
BEGIN CATCH END CATCH
GO
