DROP PROCEDURE IF EXISTS [dbo].[DataSets_Relationships_GetList]
GO
CREATE PROCEDURE [dbo].[DataSets_Relationships_GetList]
	@parentId int
AS
	SELECT r.*, 
	parent.[label] AS parentLabel,
	child.[label] AS childLabel,
	parent.tableName AS parentTableName,
	child.tableName AS childTableName
	FROM Datasets_Relationships r
	CROSS APPLY (SELECT * FROM Datasets WHERE datasetId=r.parentId) AS parent
	CROSS APPLY (SELECT * FROM Datasets WHERE datasetId=r.childId) AS child
	WHERE r.parentId=@parentId ORDER BY childTableName ASC