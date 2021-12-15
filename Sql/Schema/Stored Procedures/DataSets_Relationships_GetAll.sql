CREATE PROCEDURE [dbo].[DataSets_Relationships_GetAll]
	
AS
	SELECT r.*, 
	parent.[label] AS parentLabel,
	child.[label] AS childLabel,
	parent.tableName AS parentTableName,
	child.tableName AS childTableName
	FROM Datasets_Relationships r
	CROSS APPLY (SELECT * FROM Datasets WHERE datasetId=r.parentId) AS parent
	CROSS APPLY (SELECT * FROM Datasets WHERE datasetId=r.childId) AS child
	ORDER BY childTableName ASC