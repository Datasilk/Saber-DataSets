BEGIN TRY
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

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

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

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

BEGIN TRY
    CREATE SEQUENCE [dbo].[SequenceDataSets]
        AS BIGINT
        START WITH 1
        INCREMENT BY 1
        NO CACHE;
END TRY
BEGIN CATCH END CATCH


/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSets_GetList]
GO
CREATE PROCEDURE [dbo].[DataSets_GetList]
	@userId int NULL = NULL,
	@all bit = 0,
	@noadmin bit = 0,
	@search nvarchar(MAX)
AS
	DECLARE @isadmin bit = 0
	IF @userId IS NOT NULL BEGIN
		--check if user is admin
		SET @isadmin = CASE WHEN EXISTS(SELECT * FROM Users WHERE userId=@userId AND isadmin=1) THEN 1 ELSE 0 END
	END
	SELECT * FROM DataSets 
	WHERE deleted = 0
	AND
	(
		-- user permissions
		(
			@userId IS NOT NULL
			AND (
				(@all = 1 AND (userId IS NULL OR userId = @userId OR @noadmin = 1))
				OR (@all = 0 AND (userId = @userId OR @noadmin = 1))
			)
		)
		OR (@isadmin = 1 AND @all = 1)
		OR (@noadmin = 1 AND @all = 1)
		OR
		(
			@userId IS NULL 
			AND userId IS NULL
		)
	)
	AND 
	(
		--  text search
		(
			@search IS NOT NULL
			AND [label] LIKE '%' + @search + '%'
		)
		OR @search IS NULL
	)
	ORDER BY tableName ASC

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSets_Relationships_GetAll]
GO
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
/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

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
	WHERE r.parentId=@parentId OR r.childId=@parentId ORDER BY childTableName ASC
/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSets_Relationship_Create]
GO
CREATE PROCEDURE [dbo].[DataSets_Relationship_Create]
	@parentId INT,
	@childId INT,
	@childColumn nvarchar(32),
	@parentList nvarchar(32),
	@listtype INT
AS
	IF EXISTS(SELECT * FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId) BEGIN
		DELETE FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId
	END

	INSERT INTO Datasets_Relationships (parentId, childId, parentList, childcolumn, listtype)
	VALUES (@parentId, @childId, @parentList, @childColumn, @listtype)
/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_AddRecord]
GO
CREATE PROCEDURE [dbo].[DataSet_AddRecord]
	@datasetId int,
	@userId int = 0,
	@recordId int = 0,
	@lang nvarchar(16),
	@fields XML 
	/* example:	
		<fields>
			<field name="label"><value>My Label</value></field>
			<field name="description"><value>The short summary of my record.</value></field>
			<field name="datecreated"><value>2/22/1983</value></field>
		</fields>
	*/
AS
SET NOCOUNT ON
	--first, get a list of column names & data types from our target data set table
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	
	SELECT c.[name] AS col, t.[Name] AS datatype
    INTO #cols 
	FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('DataSet_' + @tableName)
	AND c.[name] NOT IN ('lang', 'userId')

	--next, get a list of fields from XML
	DECLARE @hdoc INT
	DECLARE @fieldlist TABLE (
		[name] nvarchar(64),
		[value] nvarchar(MAX)
	)
	EXEC sp_xml_preparedocument @hdoc OUTPUT, @fields;

	INSERT INTO @fieldlist
	SELECT x.[name], x.[value]
	FROM (
		SELECT * FROM OPENXML( @hdoc, '//field', 2)
		WITH (
			[name] nvarchar(32) '@name',
			[value] nvarchar(MAX)
		)
	) AS x

	--build SQL string from XML fields
	DECLARE @newId nvarchar(MAX) ='DECLARE @newId int = ' + 
		(CASE WHEN @recordId > 0 THEN CONVERT(nvarchar(16), @recordId) 
		ELSE '0; SET @newId = NEXT VALUE FOR [Sequence_DataSet_' + @tableName END) + '];'

	DECLARE @sql nvarchar(MAX) = @newId + 'INSERT INTO [DataSet_' + @tableName + '] (Id, lang, userId, datecreated, datemodified, ',
	@values nvarchar(MAX) = 'VALUES (@newId, ''' + @lang + ''', ' + 
		(CASE WHEN @userId IS NULL THEN 'NULL' ELSE CONVERT(nvarchar(16), @userId) END) +
		', GETUTCDATE(), GETUTCDATE(), ',
	@name nvarchar(64), @value nvarchar(MAX), 
	@cursor CURSOR, @datatype varchar(16),
	@cols int = 0

	SET @cursor = CURSOR FOR
	SELECT [name], [value] FROM @fieldlist
	OPEN @cursor
	FETCH NEXT FROM @cursor INTO @name, @value
	WHILE @@FETCH_STATUS = 0 BEGIN
		--get data type for column
		SET @datatype = ''
		SELECT @datatype = datatype FROM #cols WHERE col=@name
		IF @datatype != '' BEGIN
			IF @cols > 0 BEGIN
				SET @sql += ', '
				SET @values += ', '
			END
			IF @datatype = 'varchar' OR @datatype = 'nvarchar' OR @datatype = 'datetime2' BEGIN
				SET @values += '''' + REPLACE(@value, '''', '''''') + ''''
			END ELSE BEGIN
				SET @values += @value
			END 
			SET @sql += '[' + @name + ']'
		END
		SET @cols += 1
		FETCH NEXT FROM @cursor INTO @name, @value
		IF @@FETCH_STATUS <> 0 BEGIN
			SET @sql += ') '
			SET @values += ')'
		END
	END
	CLOSE @cursor
	DEALLOCATE @cursor

	--finally, execute SQL string
	SET @sql += @values
	EXECUTE sp_executesql @sql



/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_Create]
GO
CREATE PROCEDURE [dbo].[DataSet_Create]
	@userId int NULL = NULL,
	@userdata bit = 0,
	@label nvarchar(64),
	@description nvarchar(MAX),
	@partialview nvarchar(255),
	@columns XML 
	/* example:	
		<columns>
			<column name="label" datatype="text" maxlength="64"></column>
			<column name="description" datatype="text" maxlength="max"></column>
			<column name="datecreated" datatype="datetime" default="now"></column>
			<column name="list-team" datatype="list" default=""></column>
			<column name="list-categories" datatype="relationship" dataset="categories" columnname="productid" listtype="0"></column>
		</columns>
	*/
AS
	IF EXISTS(SELECT * FROM DataSets WHERE [label]=@label) RETURN

	DECLARE @tablename nvarchar(64) = REPLACE(@label, ' ', '_');

	--first, create a new sequence for the dataset
	DECLARE @sql nvarchar(MAX) = 'CREATE SEQUENCE [dbo].[Sequence_DataSet_' + @tableName + '] AS BIGINT START WITH 1 INCREMENT BY 1 NO CACHE'
	IF NOT EXISTS(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[Sequence_DataSet_' + @tableName + ']') AND [type] = 'SO') BEGIN
		EXECUTE sp_executesql @sql
	END
		


	--create a new table for this dataset
	SET @sql = 'CREATE TABLE [dbo].[DataSet_' + @tablename + '] (Id INT, lang NVARCHAR(16), userId int NOT NULL DEFAULT 1, ' + 
				'datecreated DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(), datemodified DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(), '
	DECLARE @sqlVars nvarchar(MAX) = ''
	DECLARE @sqlVals nvarchar(MAX) = ''
	DECLARE @indexes nvarchar(MAX) = 'CREATE INDEX [IX_DataSet_' + @tableName + '_DateCreated] ON [dbo].[DataSet_' + @tableName + '] ([datecreated])' +
				'CREATE INDEX [IX_DataSet_' + @tableName + '_DateModified] ON [dbo].[DataSet_' + @tableName + '] ([datemodified])'
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
			SET @sql = @sql + '[' + @name + '] INT NULL ' + CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default END
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'decimal' BEGIN
			SET @sql = @sql + '[' + @name + '] DECIMAL(18,0) NULL ' + CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default END
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
			SET @newname = REPLACE(@name, '-', '_')
			SET @sql = @sql + '[' + @newname + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
			IF @listtype = 0 SET @columnname = ''
			SET @relationships = @relationships + 'EXEC DataSets_Relationship_Create @parentId=#datasetId#, @childId=' + @dataset + ', @parentList=''' + @name + ''', @childColumn=''' + @columnname + ''', @listtype=' + @listtype + CHAR(13) 
		END
		IF @datatype = 'relationship-id' BEGIN
			SET @sql = @sql + '[' + @name + '] INT NOT NULL DEFAULT 0'
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'list' BEGIN
			SET @newname = REPLACE(@name, '-', '_')
			SET @sql = @sql + '[' + @newname + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
		END
		FETCH NEXT FROM @cursor INTO @name, @datatype, @maxlength, @default, @dataset, @columnname, @listtype
		SET @sql = @sql + ', '
	END
	CLOSE @cursor
	DEALLOCATE @cursor

	SET @sql = @sql + 'PRIMARY KEY (Id, lang))'
	PRINT @sql
	PRINT @indexes

	--execute generated SQL code
	EXECUTE sp_executesql @sql
	EXECUTE sp_executesql @indexes

	--finally, record dataset info
	INSERT INTO DataSets (userId, [label], tableName, partialview, [description], datecreated, userdata, deleted)
	VALUES (@userId, @label, @tablename, @partialview, @description, GETUTCDATE(), @userdata, 0)

	DECLARE @datasetId int
	SELECT @datasetId = datasetId FROM DataSets WHERE tableName=@tablename
		
	IF @relationships != '' BEGIN
		DECLARE @finalsql nvarchar(MAX) = REPLACE(@relationships, '#datasetId#', CONVERT(nvarchar(MAX), @datasetId))
		EXECUTE sp_executesql @finalsql
	END

	SELECT @datasetId

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_Delete]
GO
CREATE PROCEDURE [dbo].[DataSet_Delete]
	@datasetId int
AS
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	DECLARE @sql nvarchar(MAX) = 'DROP TABLE [DataSet_' + @tableName + ']'
	EXEC sp_executesql @sql
	SET @sql = 'DROP SEQUENCE [Sequence_DataSet_' + @tableName + ']'
	EXEC sp_executesql @sql
	DELETE FROM DataSets WHERE datasetId=@datasetId
	DELETE FROM DataSets_Relationships WHERE parentId=@datasetId OR childId=@datasetId


/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_DeleteRecord]
GO
CREATE PROCEDURE [dbo].[DataSet_DeleteRecord]
	@datasetId int,
	@recordId int
AS
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	DECLARE @sql nvarchar(MAX) = 'DELETE FROM [DataSet_' + @tableName + '] WHERE Id=' + CONVERT(nvarchar(MAX), @recordId)
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
			SET @sql = 'DELETE FROM [DataSet_' + @tableName + '] WHERE ' + @column + '=' + CONVERT(nvarchar(MAX), @recordId)
			PRINT @sql
			EXEC sp_executesql @sql
		END
		FETCH FROM @cursor INTO @childId, @column
	END
	CLOSE @cursor
	DEALLOCATE @cursor

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_GetAllColumns]
GO
CREATE PROCEDURE [dbo].[DataSet_GetAllColumns]
AS
	SELECT d.datasetId, columns.[Name] FROM DataSets d
	CROSS APPLY(
		SELECT c.[name] AS [Name]
		FROM sys.columns c
		WHERE c.object_id = OBJECT_ID('DataSet_' + d.tableName)
		AND c.[name] NOT IN ('id', 'lang', 'userId', 'datecreated', 'datemodified')
	) AS columns
/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

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

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_GetInfo]
GO
CREATE PROCEDURE [dbo].[DataSet_GetInfo]
	@datasetId int
AS
	SELECT * FROM DataSets WHERE datasetId=@datasetId

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

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
			SET @sql = @sql + '[' + @name + '] INT NULL ' + CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default END
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'decimal' BEGIN
			SET @sql = @sql + '[' + @name + '] DECIMAL(18,0) NULL ' + CASE WHEN @default IS NOT NULL AND @default != '' THEN 'DEFAULT ' + @default END
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
			SET @newname = REPLACE(@name, '-', '_')
			SET @sql = @sql + '[' + @newname + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
			SET @relationships = @relationships + 'EXEC DataSets_Relationship_Create @parentId=#datasetId#, @childId=' + @dataset + ', @parentList=''' + @name + ''', @childColumn=''' + @columnname + ''', @listtype=' + @listtype + CHAR(13) 
		END
		IF @datatype = 'relationship-id' BEGIN
			SET @sql = @sql + '[' + @name + '] INT NOT NULL DEFAULT 0'
			SET @indexes = @indexes + 'CREATE INDEX [IX_DataSet_' + @tableName + '_' + @name + '] ON [dbo].[DataSet_' + @tableName + '] ([' + @name + '])'
		END
		IF @datatype = 'list' BEGIN
			SET @newname = REPLACE(@name, '-', '_')
			SET @sql = @sql + '[' + @newname + '] NVARCHAR(MAX) NOT NULL DEFAULT '''''
		END
		FETCH NEXT FROM @cursor INTO @name, @datatype, @maxlength, @default, @dataset, @columnname, @listtype
		SET @sql = @sql + ', '
	END
	CLOSE @cursor
	DEALLOCATE @cursor

	PRINT @sql
	PRINT @indexes

	--execute generated SQL code
	EXECUTE sp_executesql @sql
	EXECUTE sp_executesql @indexes
	IF @relationships != '' BEGIN
		EXECUTE sp_executesql @relationships
	END

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_UpdateInfo]
GO
CREATE PROCEDURE [dbo].[DataSet_UpdateInfo]
	@datasetId int,
	@userId int NULL,
	@userdata bit = 0,
	@label nvarchar(64),
	@description nvarchar(MAX)
AS
	DECLARE @uid int
	SELECT @uid = userId FROM DataSets WHERE datasetId=@datasetId
	IF @uid IS NOT NULL AND @userId IS NOT NULL SET @userId = @uid -- make sure to keep original dataset owner
	UPDATE DataSets SET userId=@userId, [label]=@label, [description]=@description, userdata=@userdata 
	WHERE datasetId=@datasetId

/* ////////////////////////////////////////////////////////////////////////////////////// */

GO

/* ////////////////////////////////////////////////////////////////////////////////////// */

DROP PROCEDURE IF EXISTS [dbo].[DataSet_UpdateRecord]
GO
CREATE PROCEDURE [dbo].[DataSet_UpdateRecord]
	@userId int,
	@datasetId int,
	@recordId int = 0,
	@lang nvarchar(16),
	@fields XML 
	/* example:	
		<fields>
			<field name="label"><value>My Label</value></field>
			<field name="description"><value>The short summary of my record.</value></field>
			<field name="datecreated"><value>2/22/1983</value></field>
		</fields>
	*/
AS
SET NOCOUNT ON
	--first, get a list of column names & data types from our target data set table
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	
	SELECT c.[name] AS col, t.[Name] AS datatype
    INTO #cols 
	FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('DataSet_' + @tableName)

	--next, get a list of fields from XML
	DECLARE @hdoc INT
	DECLARE @fieldlist TABLE (
		[name] nvarchar(64),
		[value] nvarchar(MAX)
	)
	EXEC sp_xml_preparedocument @hdoc OUTPUT, @fields;

	INSERT INTO @fieldlist
	SELECT x.[name], x.[value]
	FROM (
		SELECT * FROM OPENXML( @hdoc, '//field', 2)
		WITH (
			[name] nvarchar(32) '@name',
			[value] nvarchar(MAX)
		)
	) AS x

	--build SQL string from XML fields
	DECLARE @sql nvarchar(MAX) = 'SELECT CASE WHEN EXISTS(SELECT * FROM [DataSet_' + @tableName + '] WHERE Id=' + CONVERT(nvarchar(16), @recordId) + ' AND lang=''' + @lang + ''') THEN 1 ELSE 0 END AS [value]',
	@name nvarchar(64), @value nvarchar(MAX), 
	@cursor CURSOR, @datatype varchar(16), @cols int = 0

	--first, check if record already exists
	DECLARE @exists TABLE (value bit)
	INSERT INTO @exists EXEC sp_executesql @sql
	IF EXISTS(SELECT * FROM @exists WHERE [value]=1) BEGIN
		--record already exists
		SET @sql = 'UPDATE [DataSet_' + @tableName + '] SET '
		SET @cursor = CURSOR FOR
		SELECT [name], [value] FROM @fieldlist
		OPEN @cursor
		FETCH NEXT FROM @cursor INTO @name, @value
		WHILE @@FETCH_STATUS = 0 BEGIN
			--get data type for column
			SET @datatype = ''
			SELECT @datatype = datatype FROM #cols WHERE col=@name
			IF @datatype != '' BEGIN
				IF @cols > 0 SET @sql += ', '
				IF @datatype = 'varchar' OR @datatype = 'nvarchar' OR @datatype = 'datetime2' BEGIN
					SET @sql += '[' + @name + '] = ''' + REPLACE(@value, '''', '''''') + ''''
				END ELSE BEGIN
					SET @sql += '[' + @name + '] = ' + @value
				END 
			END
			SET @cols += 1
			FETCH NEXT FROM @cursor INTO @name, @value
		END
		CLOSE @cursor
		DEALLOCATE @cursor

		--finally, execute SQL string
		SET @sql += ', datemodified=GETUTCDATE() WHERE Id=' + CONVERT(nvarchar(16), @recordId) + ' AND lang=''' + @lang + ''''
		EXEC sp_executesql @sql
		
	END ELSE BEGIN
		--create new record
		EXEC DataSet_AddRecord @datasetId=@datasetId, @recordId=@recordId, @lang=@lang, @fields=@fields
	END