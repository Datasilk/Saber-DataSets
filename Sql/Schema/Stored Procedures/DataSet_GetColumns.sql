DROP PROCEDURE IF EXISTS [dbo].[DataSet_GetColumns]
GO
CREATE PROCEDURE [dbo].[DataSet_GetColumns]
	@datasetId int
AS
	DECLARE @tableName nvarchar(64)
	SELECT @tableName = tableName FROM DataSets WHERE datasetId=@datasetId
	SELECT c.[name], t.name AS datatype
	FROM sys.columns c
	JOIN sys.types t ON t.system_type_id = c.system_type_id AND t.system_type_id = t.user_type_id
	WHERE c.object_id = OBJECT_ID('DataSet_' + @tableName)
	AND c.[name] NOT IN ('id', 'lang', 'userId', 'datecreated', 'datemodified')
