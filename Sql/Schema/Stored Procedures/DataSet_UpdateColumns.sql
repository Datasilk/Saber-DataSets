DROP PROCEDURE IF EXISTS [dbo].[DataSet_UpdateColumns]
GO
CREATE PROCEDURE [dbo].[DataSet_UpdateColumns]
	@datasetId int,
	@columns XML 
	/* example:	
		<columns>
			<column name="label" datatype="text" maxlength="64"></column>
			<column name="description" datatype="text" maxlength="max"></column>
			<column name="datecreated" datatype="datetime" default="now"></column>
			<column name="list-team" datatype="list" default=""></column>
		</columns>
	*/
AS

	DECLARE @tablename nvarchar(64)
	SELECT @tablename = tableName FROM DataSets WHERE datasetId=@datasetId

	--update existing table for this dataset
	DECLARE @sql nvarchar(MAX) = 'ALTER TABLE [dbo].[DataSet_' + @tablename + '] ADD '
	DECLARE @indexes nvarchar(MAX) = ''
	DECLARE @relationships nvarchar(MAX) = ''
		
	DECLARE @hdoc INT
	DECLARE @cols TABLE (
		[name] nvarchar(32),
		datatype varchar(32),
		[maxlength] varchar(32),
		[default] varchar(32),
		[dataset] varchar(32),
		[columnname] varchar(32),
		[listtype] varchar(32) -- 0 = filtered list, 1 = single selection
	)
	EXEC sp_xml_preparedocument @hdoc OUTPUT, @columns;

	INSERT INTO @cols
	SELECT x.[name], x.datatype, x.[maxlength], x.[default], x.[dataset], x.[columnname], x.[listtype]
	FROM (
		SELECT * FROM OPENXML( @hdoc, '//column', 2)
		WITH (
			[name] nvarchar(32) '@name',
			datatype nvarchar(32) '@datatype',
			[maxlength] nvarchar(32) '@maxlength',
			[default] nvarchar(32) '@default',
			[dataset] nvarchar(32) '@dataset',
			[columnname] nvarchar(32) '@columnname',
			[listtype] nvarchar(32) '@listtype'
		)
	) AS x
	
	DECLARE @cursor CURSOR 
	DECLARE @name nvarchar(32), @datatype nvarchar(32), @maxlength nvarchar(32), 
		@default nvarchar(32), @dataset nvarchar(32), @columnname nvarchar(32), @listtype nvarchar(32),
		@newname nvarchar(32)
	SET @cursor = CURSOR FOR
	SELECT [name], [datatype],[maxlength], [default], [dataset], [columnname], [listtype] FROM @cols
	OPEN @cursor
	FETCH NEXT FROM @cursor INTO @name, @datatype, @maxlength, @default, @dataset, @columnname, @listtype
	WHILE @@FETCH_STATUS = 0 BEGIN
		SET @maxlength = ISNULL(@maxlength, '64')
		IF @maxlength = '0' SET @maxlength = 'MAX'
		IF @datatype = 'text' BEGIN
			SET @sql = @sql + '[' + @name + '] NVARCHAR(' + @maxlength + ') NOT NULL DEFAULT '''''
			IF @maxlength != 'max' BEGIN
				SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
			END
		END
		IF @datatype = 'image' BEGIN
			SET @sql = @sql + '[' + @name + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
		END
		IF @datatype = 'number' BEGIN
			SET @sql = @sql + '[' + @name + '] INT NULL ' + (CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default ELSE '' END)
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'decimal' BEGIN
			SET @sql = @sql + '[' + @name + '] DECIMAL(18,0) NULL ' + (CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default ELSE '' END)
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'bit' BEGIN
			SET @sql = @sql + '[' + @name + '] BIT NOT NULL DEFAULT ' + CASE WHEN @default = '1' THEN '1' ELSE '0' END
		END
		IF @datatype = 'datetime' BEGIN
			SET @sql = @sql + '[' + @name + '] DATETIME2(7) ' + CASE WHEN @default = 'now' THEN 'NOT NULL DEFAULT GETUTCDATE()' ELSE 'NULL' END
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'relationship' BEGIN
			SET @sql = @sql + '[' + @name + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
			SET @relationships = @relationships + 'EXEC DataSets_Relationship_Create @parentId=' + CAST(@datasetId AS nvarchar(MAX)) + ', @childId=' + @dataset + ', @parentList=''' + @name + ''', @childColumn=''' + @columnname + ''', @listtype=' + @listtype + CHAR(13) 
		END
		IF @datatype = 'relationship-id' BEGIN
			SET @sql = @sql + '[' + @name + '] INT NOT NULL DEFAULT 0'
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'list' BEGIN
			SET @sql = @sql + '[' + @name + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
		END
		FETCH NEXT FROM @cursor INTO @name, @datatype, @maxlength, @default, @dataset, @columnname, @listtype
		IF @@FETCH_STATUS = 0 SET @sql = @sql + ', '
	END
	CLOSE @cursor
	DEALLOCATE @cursor

	PRINT @sql
	PRINT @indexes

	--execute generated SQL code
	EXECUTE sp_executesql @sql
	EXECUTE sp_executesql @indexes
	IF @relationships != '' BEGIN
		PRINT @relationships
		EXECUTE sp_executesql @relationships
	END
