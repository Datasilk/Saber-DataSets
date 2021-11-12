CREATE PROCEDURE [dbo].[DataSet_GetAllColumns]
AS
	SELECT d.datasetId, columns.[Name] FROM DataSets d
	CROSS APPLY(
		SELECT c.[name] AS [Name]
		FROM sys.columns c
		WHERE c.object_id = OBJECT_ID('DataSet_' + d.tableName)
		AND c.[name] NOT IN ('id', 'lang', 'userId', 'datecreated', 'datemodified')
	) AS columns