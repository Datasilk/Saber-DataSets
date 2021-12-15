CREATE PROCEDURE [dbo].[DataSet_DeleteRecord]
	@datasetId int,
	@recordId int
AS
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	DECLARE @sql nvarchar(MAX) = 'DELETE FROM DataSet_' + @tableName + ' WHERE Id=' + CONVERT(nvarchar(MAX), @recordId)
	EXEC sp_executesql @sql

	DECLARE @cursor CURSOR, @column nvarchar(32), @childId int
	SET @cursor = CURSOR FOR
	SELECT childId, childColumn FROM DataSets_Relationships
	WHERE parentId=@datasetId
	OPEN @cursor
	FETCH FROM @cursor INTO @childId, @column
	WHILE @@FETCH_STATUS = 0 BEGIN
		-- delete all records from relationship tables that reference recordId
		SET @tableName = ''
		SELECT @tableName = tableName FROM DataSets WHERE datasetId=@childId
		IF @tableName != '' BEGIN
			SET @sql = 'DELETE FROM DataSet_' + @tableName + ' WHERE ' + @column + '=' + CONVERT(nvarchar(MAX), @recordId)
			PRINT @sql
			EXEC sp_executesql @sql
		END
		FETCH FROM @cursor INTO @childId, @column
	END
	CLOSE @cursor
	DEALLOCATE @cursor
